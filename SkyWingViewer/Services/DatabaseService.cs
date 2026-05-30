using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RTools_NTS.Util;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Text;
using System.Windows.Shapes;

namespace SkyWingViewer.Services;


//TODO: scope 作ったりって実は軽量らしく、そのため同じ　context を使いまわしてもよさそうらしい。結構書いてしまってから気づいたので、余裕があるときにリファクタリングする
//TODO: 不慣れなので１つ１つ試し試し作ったが、そのせいで共通化できる部分などが非常に多いので、余裕ができたらリファクタリングする。一旦実装優先とする。

//TODO: AsNoTracking を使うと読み取りパフォーマンスが向上するので、読み取りのみのクエリ全般に付与すること
//ex) await context.Assets.AsNoTracking().AnyAsync(a => a.Path == target.Path))
//集計や、直接取得した値を利用する場合に付与
//逆に、取得したエンティティを編集（書き込みや削除）して保存するような場合は、結果的に書き込みワークロードを含むので付与しないこと

//TODO: AAA.BBB とつなげられるような実装にするとアセット読み込む処理とか一回書けば終わるかもしれない。やり方を調べて検討

//TODO: set,get の使い方とか少し一貫性が無い部分があるので修正

/* TODO: 下記のようにする方が効率が良い様子
 * private readonly IDbContextFactory<MyDbContext> _factory;
 * using var context = await _factory.CreateDbContextAsync();
 */
public class DatabaseService
{

    private IServiceProvider _serviceProvider;

    public DatabaseService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    /* *************** 登録 ************** */


    public async Task RegistrationAssetAsync(FileSystemItemBase target)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //既存かチェック
            if (await context.Assets.AnyAsync(a => a.Path == target.Path))
            {
                return;
            }

            AssetEntity asset = CreateAssetEntity(target);

            //書き込みを実行
            context.Assets.Add(asset);
            await context.SaveChangesAsync();
        }
    }

    public async Task RegistrationTagAsync(string tagName)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            TagsEntity tag = CreateTagEntity(tagName);

            if (await context.Tags.AnyAsync(t => t.TagName == tagName))
            {
                return;
            }

            //書き込みを実行
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
        }
    }

    public async Task BulkRegistrationAssetAsync(IEnumerable<FileSystemItemBase> items)
    {
        List<AssetEntity> assets = new();
        foreach (var item in items)
        {
            assets.Add(CreateAssetEntity(item));
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            try
            {
                //TODO: 速度比較
                //トラッカーをオフにすると速度面で改良できる場合がある
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                //TODO: 速度比較
                //context.Assets.AddRange(assets);
                //context.BulkInsert(assets);

                await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path) },
                    ////更新対象
                    //PropertiesToInclude = new List<string> {
                    //    nameof(AssetEntity.Name),
                    //    nameof(AssetEntity.FileSize),
                    //    nameof(AssetEntity.Type),
                    //    nameof(AssetEntity.Path),
                    //    nameof(AssetEntity.ParentPath),
                    //    nameof(AssetEntity.CreationFileTime),
                    //    nameof(AssetEntity.ModifiedTime),
                    //    nameof(AssetEntity.CapturedTime),
                    //    nameof(AssetEntity.AddedTime),
                    //    nameof(AssetEntity.Width),
                    //    nameof(AssetEntity.Height),
                    //    nameof(AssetEntity.Rating),
                    // }

                    //update になる場合に更新しないカラム
                    PropertiesToExcludeOnUpdate = new List<string>
                    {
                        nameof(AssetEntity.AddedTime),
                        nameof(AssetEntity.Memo)
                    }
                });

                //BulkInsertOrUpdateAsync では DB への反映まで終わっているので不要
                //await context.SaveChangesAsync();
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
    }

    public async Task BulkSetRateAsync(IEnumerable<FileSystemItemBase> items,int rate)
    {
        //一旦 AssetEntity を作ってレートをセットする
        List<AssetEntity> assets = new();
        AssetEntity asset;
        foreach (var item in items)
        {
            asset = CreateAssetEntity(item);
            asset.Rating = rate;
            assets.Add(asset);
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            try
            {
                //トラッカーをオフにすると速度面で改良できる場合がある
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                //登録済みの物はレートのみ更新する
                await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path) },

                    PropertiesToIncludeOnUpdate = new List<string> 
                    {
                        nameof(AssetEntity.Rating)
                    },
                });
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
    }

    public async Task SetMemoAsync(IEnumerable<FileSystemItemBase> items,string memo)
    {

        List<AssetEntity> assets = new();
        AssetEntity asset;
        foreach(var item in items)
        {
            asset = CreateAssetEntity(item);
            asset.Memo = memo;
            assets.Add(asset);
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            try
            {
                //トラッカーをオフにすると速度面で改良できる場合がある
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                //登録済みの物はレートのみ更新する
                await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path) },

                    PropertiesToIncludeOnUpdate = new List<string>
                    {
                        nameof(AssetEntity.Memo)
                    },
                });
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
    }

    /* *************** 削除 ************** */

    public async Task UnregisterAssetAsync(FileSystemItemBase target)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            AssetEntity? asset = await context.Assets.FirstOrDefaultAsync(asset => asset.Path == target.Path);

            if (asset == null)
            {
                return;
            }

            context.Assets.Remove(asset);
            await context.SaveChangesAsync();
        }
    }

    public async Task UnregisterTagAsync(string tagName)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            TagsEntity? tag = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

            if (tag == null)
            {
                return;
            }
            //書き込みを実行
            context.Tags.Remove(tag);
            await context.SaveChangesAsync();
        }
    }
    /* *************** タグの紐づけ関係 ************** */


    //TODO: bulk 版だけで良い気もする。他のも
    public async Task AttachTagToAssetAsync(FileSystemItemBase item, string tagName)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            AssetEntity? assetEntity = await context.Assets.FirstOrDefaultAsync(a => a.Path == item.Path);
            TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

            //登録が無いものへの紐づけ要請がきたら、登録してから紐づけ
            if (assetEntity == null)
            {
                assetEntity = CreateAssetEntity(item);
                context.Assets.Add(assetEntity);
            }
            if (tagsEntity == null)
            {
                tagsEntity = CreateTagEntity(tagName);
                context.Tags.Add(tagsEntity);
            }

            //新規登録をした場合には、一旦 DB に反映する。ID は自動生成なので、DB に反映するまで未確定かつ、アセットとタグのペアは ID で管理するので、未発行状態では困るため
            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }

            //アセット - タグの組み合わせで重複がないか確認
            if (await context.AssetTagPairs.AnyAsync(a => a.AssetId == assetEntity.Id && a.TagId == tagsEntity.Id) == true)
            {
                return;
            }

            AssetTagPairEntity assetTagPairEntity = new AssetTagPairEntity();
            assetTagPairEntity.AssetId = assetEntity.Id;
            assetTagPairEntity.TagId = tagsEntity.Id;
            context.AssetTagPairs.Add(assetTagPairEntity);
            await context.SaveChangesAsync();
        }
    }

    public async Task BulkAttachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        if(items.Count() == 0 || tagName.IsNullOrEmpty() == true)
        {
            return;
        }
        //受け取ったアセットのパスのリスト
        HashSet<string> assetPaths = items.Select(a => a.Path).ToHashSet();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                //DB から登録済みの物を取得
                List<AssetEntity>? assetEntity = await context.Assets.Where(a => assetPaths.Contains(a.Path)).ToListAsync();
                TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

                //登録済みのパスをハッシュセット -> 差分を取って、DB に無いアセットのパスをハッシュセットに
                HashSet<string> existsAssetPaths = assetEntity.Select(a => a.Path).ToHashSet();
                HashSet<string> newAssetPaths = assetPaths.Where(path => !existsAssetPaths.Contains(path)).ToHashSet();

                //登録が無いものへの紐づけ要請がきたら、登録して、assetEntity リストに追加
                if (newAssetPaths.Any())
                {
                    List<FileSystemItemBase> newAssets = items.Where(a => newAssetPaths.Contains(a.Path)).ToList();
                    List<AssetEntity> newAssetEntity = newAssets.Select(a => CreateAssetEntity(a)).ToList();

                    await context.BulkInsertAsync(newAssetEntity, new BulkConfig
                    {
                        //重複確認のキー
                        UpdateByProperties = new List<string> { nameof(AssetEntity.Path) },
                        //新しく登録された行と対応する Entity に対して、割り振られた ID をセットする。
                        SetOutputIdentity = true,
                    });

                    assetEntity.AddRange(newAssetEntity);
                }
                if (tagsEntity == null)
                {
                    tagsEntity = CreateTagEntity(tagName);
                    context.Tags.Add(tagsEntity);
                }

                //新規登録をした場合には、一旦 DB に反映する。ID は自動生成なので、DB に反映するまで未確定かつ、アセットとタグのペアは ID で管理するので、未発行状態では困るため
                if (context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }

                //新規登録分の assetTagPairEntitys のリストの作成
                List<AssetTagPairEntity> assetTagPairEntitys = assetEntity.Select(asset => CreateAssetTagPairEntity(asset, tagsEntity)).ToList();

                await context.BulkInsertOrUpdateAsync(assetTagPairEntitys, new BulkConfig
                {
                    //重複している場合はスルー
                    PropertiesToIncludeOnUpdate = new List<string> { "" },
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetTagPairEntity.TagId), nameof(AssetTagPairEntity.AssetId) },

                    PropertiesToInclude = new List<string>
                {
                    nameof(AssetTagPairEntity.TagId),
                    nameof(AssetTagPairEntity.AssetId)
                }
                });
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
    }

    public async Task DetachTagToAssetAsync(FileSystemItemBase item, string tagName)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            AssetEntity? assetEntity = await context.Assets.FirstOrDefaultAsync(a => a.Path == item.Path);
            TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

            //念のため
            if (assetEntity == null || tagsEntity == null)
            {
                return;
            }

            await context.AssetTagPairs.Where(pair => pair.AssetId == assetEntity.Id && pair.TagId == tagsEntity.Id).ExecuteDeleteAsync();
            await context.SaveChangesAsync();
        }
    }

    public async Task BulkDetachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        if (items.Count() == 0 || tagName.IsNullOrEmpty() == true)
        {
            return;
        }
        //受け取ったアセットのパスのリスト
        HashSet<string> assetPaths = items.Select(a => a.Path).ToHashSet();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                //DB から登録済みの物を取得
                List<AssetEntity>? assetEntity = await context.Assets.Where(a => assetPaths.Contains(a.Path)).ToListAsync();
                TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

                //登録済みのパスをハッシュセット -> 差分を取って、DB に無いアセットのパスをハッシュセットに
                HashSet<string> existsAssetPaths = assetEntity.Select(a => a.Path).ToHashSet();

                //タグが登録されてないものか、対象のアセットが DB に存在しないならタグ解除も自動的に必要が無い
                if (tagsEntity == null || assetEntity.Any() == false)
                {
                    return;
                }

                //削除する assetTagPairEntitys のリストの作成
                List<AssetTagPairEntity> assetTagPairEntitys = assetEntity.Select(asset => CreateAssetTagPairEntity(asset, tagsEntity)).ToList();

                await context.BulkDeleteAsync(assetTagPairEntitys, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetTagPairEntity.TagId), nameof(AssetTagPairEntity.AssetId) },
                });
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
    }
    /* *************** 読み取り ************** */

    //全てのタグを取得
    public async Task<List<ItemTagModel>> GetAllTagsAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //List<string> tags = await context.Tags.Select(t => t.TagName).ToListAsync();
            List<ItemTagModel> tags = await context.Tags.Select(t => new ItemTagModel(t.TagName, t.Id)).ToListAsync();
            return tags;
        }
    }

    //Asset から、紐づいているタグのリストを string で取得
    public async Task<List<string>> GetTagsFromAssetPathAsync(FileSystemItemBase item)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //Where で item.Path と AssetEntity のカラム Path が一致する行に絞り込み -> その行の集団（つまり特定の AssetId の行が抜き出されたもの）に対して、対応する TagsEntity の TagName を取り出し、リストにして返却
            List<string> tags = await context.AssetTagPairs.Where(a => a.AssetEntity.Path == item.Path).Select(a => a.TagsEntity.TagName).ToListAsync();
            return tags;
        }
    }


    //Asset の一覧から、全てに紐づいているタグのリストを string で取得
    public async Task<HashSet<string>> GetIntersectTagsFromAssetListAsync(IEnumerable<FileSystemItemBase> assets)
    {
        //検索が早いハッシュセットに
        HashSet<string> assetPaths = assets.Select(a => a.Path).ToHashSet();


        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();


            //まずは対象アセットの ID を取得する
            HashSet<int> assetIds = new();
            assetIds = await context.Assets.Where(a => assetPaths.Contains(a.Path)).Select(a => a.Id).ToHashSetAsync();

            //DB に登録されたアセットが無いか、１つでも未登録ならすべてに登録されたタグは無い
            if (assetIds.Count == 0 || assetIds.Count != assetPaths.Count)
            {
                return new HashSet<string>();
            }

            List<string> tags = new();

            /*
             * assetIds に含まれる ID を持つ行に絞り込み 
             * -> Tag 毎でグループ化（TagA : 関連づけられてる AssetId のリスト）を抜き出す感じ。TagId と TagName は 1:1 なので、グループ化に影響なし
             * -> その中から、関連付けられてる AssetId と assetId の数が同じものに絞り込み（つまり、全てのアセットに関連付けられたタグ）
             * -> 残った行の、タグ名を取得
             * GroupBy でこのようにしているのは、GroupBy 後は TagsEntity にアクセスできないため
             */
            tags = await context.AssetTagPairs
                .Where(pair => assetIds.Contains(pair.AssetId))
                .GroupBy(t => new { t.TagId, t.TagsEntity.TagName })
                .Where(t => t.Count() == assetIds.Count)
                .Select(tags => tags.Key.TagName)
                .ToListAsync();

            HashSet<string> result = tags.ToHashSet();

            return result ?? new HashSet<string>();
        }

    }

    // 文字列に対して、そのタグが存在するかを取得
    public async Task<bool> IsTagExistsAsync(string tagName)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            return await context.Tags.AnyAsync(t => t.TagName == tagName);
        }
    }


    public async Task<HashSet<string>> GetAssetPathsByAllTagsAndParentPathAsync(IEnumerable<ItemTagModel> tags,string parentPath)
    {
        if ((tags.Count() < 1 || tags == null) || parentPath == null) return new HashSet<string>();

        List<int> tagIds = tags.Select(tag => tag.TagId).ToList();
        //int tagCnt = tagIds.Count();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();


            /*
             
                最初の考えたクエリ。Count をアセット数分することになるのと、GroupBy をする必要がある。
                Count 時にカウントアップのみだが、全行を探索
                context.AssetTagPairs.Where(pair => pair.AssetEntity.ParentPath == parentPath).Where(pair => tagIds.Contains(pair.TagsEntity.Id))
                .GroupBy(pair => new { pair.AssetId ,pair.AssetEntity.Path})
                .Where(a => a.Count() == tagCnt)
                .Select(a => a.Key.Path)
                .ToListAsync();

                Assets から。初期の絞り込みは良さそうではあるが、アセットの数だけサブクエリを回すので良くない印象がある
                context.Assets.Where(a => a.ParentPath == parentPath)
                .Where(a => a.AssetTagPairs
                .Where(pair => tagIds.Contains(pair.TagsEntity.Id))
                .Count() == tagCnt)
                .Select(a => a.Path)
                .ToListAsync();
             */
    
            /*
             * 始めに対象アセットだけ絞り込んで、そのあとは各タグに対して Any をしていく。
             * Any の流れ次第だが、進むにつれて探索対象をカットしていける。
             * Count や GroupBy は回避できてるので、これが早い気がする。
             */
            var query = context.Assets.Where(a => a.ParentPath == parentPath);

            //WILL: 最適化が必要なら、タグに紐づいたアセットの件数を保持するように。もっと必要なら、ディレクトリごと等で保持する事も検討
            //どうしても最適化がもっといるなら、付与されてるアセットが少ないタグの順番で探索すると良い
            //が、タグが何件紐づいてるか別にテーブルが必要そう
            foreach (var id in tagIds)
            {
                // 各タグが「存在する」ことを条件として追加していく
                query = query.Where(a => a.AssetTagPairs.Any(pair => pair.TagId == id));
            }

           　return await query.Select(a => a.Path).ToHashSetAsync();
        }

    }

    //選択中のアセットのレート取得
    public async Task<int> GetRateFromAssetsAsync(IEnumerable<FileSystemItemBase> assets)
    {
        HashSet<string> assetPaths = assets.Select(a => a.Path).ToHashSet();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //Rate について取得後 Distinct で重複を排除
            List<int> rates = await context.Assets.Where(a => assetPaths.Contains(a.Path)).Select(a => a.Rating).Distinct().ToListAsync();

            //重複排除後１行なら単一の評価をされた集団。
            if(rates.Count == 1)
            {
                return rates[0];
            }
            else
            {
                //複数の評価が含まれる場合
                return 0;
            }
        }
    }

    //あるディレクトリのアセットのレートを、パスとレートの辞書で返す
    public async Task<Dictionary<string,int>> GetRateFromParentPathAsync(IEnumerable<FileSystemItemBase> assets,string parentPath)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            /*
             * 始めに対象アセットだけ絞り込んで、そのあとは各タグに対して Any をしていく。
             * Any の流れ次第だが、進むにつれて探索対象をカットしていける。
             * Count や GroupBy は回避できてるので、これが早い気がする。
             */

            return await context.Assets.Where(a => a.ParentPath == parentPath)
                .Select(a => new {a.Path, a.Rating})
                .ToDictionaryAsync(a => a.Path, a => a.Rating);
            
        }
    }


    //受け取った assets について、DB に登録されているものに対して、レートとタグを直接セット
    public async Task SetTagAndRateToFileSystemBaseModelAsync(IEnumerable<FileSystemItemBase> assets)
    {
        HashSet<string> assetPashs = assets.Select(a => a.Path).ToHashSet();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //Dictionary<string, AssetEntity> assetEntitys = await context.Assets.Where(a => assetPashs.Contains(a.Path)).ToDictionaryAsync(a => a.Path, a => a);

            ////まず AssetTagPairs から対象のアセットに関連する行に絞り込み -> アセット Path でグループ化 -> Path, tagList に select -> 辞書に変換
            //Dictionary<string, List<string>> assetTags = await context.AssetTagPairs
            //    .Where(pair => assetEntitys.ContainsKey(pair.AssetEntity.Path))
            //    .GroupBy(pair => pair.AssetEntity.Path)
            //    .Select(g => new { Path = g.Key, Tags = g.Select(pair => pair.TagsEntity.TagName).ToList() })
            //    .ToDictionaryAsync(t => t.Path, t => t.Tags);


            //foreach (FileSystemItemBase asset in assets)
            //{
            //    if(assetEntitys.TryGetValue(asset.Path, out AssetEntity? entity))
            //    {
            //        asset.Metadata.Rating = entity.Rating;
            //    }
            //    if(assetTags.TryGetValue(asset.Path,out List<string>? tags))
            //    {
            //        asset.Metadata.Tags = tags;
            //    }
            //}

            //受け取ったアセットリストと対応する行のみを asset テーブルから select -> 匿名型を作成、中身はアセットパス、レート、中間テーブルをたどって取得可能なタグ名のリスト -> パスを Key に指定、作成した匿名型自体を value にして、辞書を作成 
            var assetDatas = await context.Assets
                .Where(a => assetPashs.Contains(a.Path))
                .Select(a => new
                {
                    Path = a.Path,
                    Rating = a.Rating,
                    Tags = a.AssetTagPairs.Select(pair => pair.TagsEntity.TagName).ToList()
                })
                .ToDictionaryAsync(a => a.Path, a => a);

            foreach(FileSystemItemBase asset in assets)
            {
                if(assetDatas.TryGetValue(asset.Path,out var Data))
                {
                    asset.Metadata.Rating = Data.Rating;
                    asset.Metadata.Tags = Data.Tags;
                }
            }
        }
    }

    public async Task<string?> GetMemoAsync(FileSystemItemBase asset)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            AssetEntity? assetEntity = context.Assets.FirstOrDefault(a => a.Path == asset.Path);

            if(assetEntity == null)
            {
                return null;
            }

            return assetEntity.Memo;
        }
    }

    /* *************** その他 ************** */


    //FileSystemItemBase から、Asset テーブルに書き込むための情報を取得格納
    public AssetEntity CreateAssetEntity(FileSystemItemBase target)
    {
        AssetEntity asset = new AssetEntity();

        //アイテム名
        asset.Name = target.Metadata.Name;

        //ファイルサイズ。ディレクトリなら格納しない
        if (target.Metadata.Length is long size)
        {
            asset.FileSize = size;
        }

        //パス
        asset.Path = target.Path;

        asset.CreationFileTime = target.Metadata.CreationFileTime;
        asset.ModifiedTime = target.Metadata.ModifiedTime;
        asset.AddedTime = DateTimeOffset.Now;

        //所属ディレクトリ
        asset.ParentPath = System.IO.Path.GetDirectoryName(target.Path) ?? "";


        //ファイルかフォルダ限定の処理
        if (target is AssetBase assetBase)
        {
            //拡張子
            asset.Type = assetBase.Extension;
        }
        else if (target is DirectoryModel)
        {
            asset.Type = "Directory";
        }

        asset.Rating = target.Metadata.Rating;

        //TODO: CapturedTime,Width,Height

        return asset;
    }

    public TagsEntity CreateTagEntity(string tagName)
    {
        TagsEntity tag = new TagsEntity();

        tag.TagName = tagName;

        return tag;
    }


    public AssetTagPairEntity CreateAssetTagPairEntity(AssetEntity assetEntity,TagsEntity tagEntity)
    {
        AssetTagPairEntity pair = new();
        pair.AssetId = assetEntity.Id;
        pair.TagId = tagEntity.Id;
        return pair;
    }








}
