using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Utilities;
using RTools_NTS.Util;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Shapes;
using static System.IO.Path;

namespace SkyWingViewer.Services;


//処理結果伝達用
public enum ResultCode
{
    Success, //成功
    AlreadyExists, //重複があるなどの利用で未処理
    TargetNotFound, //DB にある想定の処理で、無かったので失敗
    Failed //失敗
}

//処理結果コードと、対応するモデルクラス等何かを返したい時に使用
public class Result<T> where T : class
{
    public ResultCode ResultCode { get; init; }
    public T? Value { get; init; }

    public Result(ResultCode resultCode, T? value)
    {
        ResultCode = resultCode;
        Value = value;
    }
}


//TODO: scope 作ったりって実は軽量らしく、そのため同じ　context を使いまわしてもよさそうらしい。結構書いてしまってから気づいたので、余裕があるときにリファクタリングする
//TODO: 不慣れなので１つ１つ試し試し作ったが、そのせいで共通化できる部分などが非常に多いので、余裕ができたらリファクタリングする。一旦実装優先とする。

//TODO: AsNoTracking を使うと読み取りパフォーマンスが向上するので、読み取りのみのクエリ全般に付与すること
//ex) await context.Assets.AsNoTracking().AnyAsync(a => a.Path == target.Path))
//集計や、直接取得した値を利用する場合に付与
//逆に、取得したエンティティを編集（書き込みや削除）して保存するような場合は、結果的に書き込みワークロードを含むので付与しないこと

//TODO: AAA.BBB とつなげられるような実装にするとアセット読み込む処理とか一回書けば終わるかもしれない。やり方を調べて検討

//TODO: set,get の使い方とか少し一貫性が無い部分があるので修正

/* TODO:  
 下記２パターンが頻出したので、どっかで共通化等検討する。FileSystemItemBase から、欲しい assetEntity を返すような感じで
```
            PathLibPair pathLibPair = ConvertPathToPathLibPair(asset.Path);
            int? libId = pathLibPair.GetLibraryId();

            AssetEntity? assetEntity = context.Assets.FirstOrDefault(a => a.Path == pathLibPair.Path && a.LibraryId == libId);
```
```
        HashSet<PathLibPair> assetPathLibPairs = assets.Select(a => ConvertPathToPathLibPair(a.Path)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
            List<AssetEntity>? assetEntity = await context.Assets.Where(a => relativePaths.Contains(a.Path)).ToListAsync();
            assetEntity = assetEntity.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.Library))).ToList();
```

 
 */

//TODO: 後から Result クラス導入したので、余裕ができたら適宜対応を進める。

/* TODO: 下記のようにする方が効率が良い様子
 * private readonly IDbContextFactory<MyDbContext> _factory;
 * using var context = await _factory.CreateDbContextAsync();
 */
public class DatabaseService
{

    private IServiceProvider _serviceProvider;
    private List<LibraryEntity> _library = new();
    private ILogger _logger;

    //未使用
    public event Action? LibraryChanged;

    public event Action? InitComplete;

    public const int OutOfLibraryId = 1;
    public const string OutOfLibraryName = "_OutOfLibrary";
    public LibraryEntity OutOfLibraryEntity = null!;



    public DatabaseService(IServiceProvider serviceProvider, ILogger<DatabaseService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public void Init()
    {
        using var scope = _serviceProvider.CreateScope();
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _library = context.Library.ToList();
        if(_library.Count == 0)
        {
            try
            {
                LibraryEntity library = new LibraryEntity();
                library.Name = OutOfLibraryName;
                library.RootPath = "";
                context.Library.Add(library);
                context.SaveChanges();
                _library.Add(library);
                OutOfLibraryEntity = library;
                
            }
            catch(Exception ex)
            {
                _logger.LogError("暗黙の初期ライブラリ生成に失敗しました。{ex}",ex);
            }

        }
        else
        {
            LibraryEntity? outOfLibraryEntity = _library.Where(l => l.Name == OutOfLibraryName).FirstOrDefault();
            if(outOfLibraryEntity == null)
            {
                _logger.LogError("暗黙のライブラリ OutOfLibrary が DB に存在しません。予期せぬ状態です。ライブラリ外に情報を登録したアセットが存在した場合、それらの情報を正常に読み取れないことが想定されます。DB に情報自体は残っていることが想定されますが、整合性の修正が必要です。");
                _logger.LogWarning("暗黙のライブラリが意図しない形で存在しなかったため、再度作成を実施します。");
                try
                {
                    LibraryEntity library = new LibraryEntity();
                    library.Name = OutOfLibraryName;
                    library.RootPath = "";
                    context.Library.Add(library);
                    context.SaveChanges();
                    _library.Add(library);
                    OutOfLibraryEntity = library;
                }
                catch (Exception ex)
                {
                    _logger.LogError("暗黙の初期ライブラリ生成に失敗しました。{ex}", ex);
                }
            }
            else
            {
                OutOfLibraryEntity = outOfLibraryEntity;
            }
        }
        InitComplete?.Invoke();
    }

    public List<LibraryModel> GetLibraryList()
    {
        List<LibraryModel> libraryList = new();
        foreach(LibraryEntity lib in _library)
        {
            if (lib.Id == OutOfLibraryId) continue;
            libraryList.Add(new LibraryModel(lib.Name, lib.RootPath));
        }
        return libraryList;
    }

    private void AddLibrary(LibraryEntity libraryEntity)
    {
        _library.Add(libraryEntity);
        LibraryChanged?.Invoke();
    }
    private void DeleteLibrary(int id)
    {
        _library.RemoveAll(l => l.Id == id);
        LibraryChanged?.Invoke();
    }

    private void UpdateLibrary(LibraryEntity libraryEntity)
    {
        foreach(LibraryEntity library in _library)
        {
            if(library.Id == libraryEntity.Id)
            {
                library.RootPath = libraryEntity.RootPath;
                LibraryChanged?.Invoke();
                return;
            }
        }
    }

    /* *************** 登録 ************** */

    //未使用だが、タグの登録のみというのは今後あり得るので一応残す
    public async Task RegistrationTagAsync(string tagName)
    {
        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        if (await context.Tags.AnyAsync(t => t.TagName == tagName))
        {
            return;
        }

        TagsEntity tag = CreateTagEntity(tagName);

        //書き込みを実行
        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        
    }


    //アセットの登録のみというのは現状利用想定が無い。(現状は何かの情報登録時に、アセットの登録自体が無いなら登録。ある場合は変更項目のみをアップデート、という処理）
    //利用する場合は、BulkInsertOrUpdateAsync の内容について確認（DB 既存のアセットについて、必要な情報を壊さないか）すること
    //public async Task BulkRegistrationAssetAsync(IEnumerable<FileSystemItemBase> items)
    //{
    //    List<AssetEntity> assets = new();
    //    foreach (var item in items)
    //    {
    //        assets.Add(CreateAssetEntity(item));
    //    }

    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        try
    //        {
    //            //TODO: 速度比較
    //            //トラッカーをオフにすると速度面で改良できる場合がある
    //            context.ChangeTracker.AutoDetectChangesEnabled = false;  

    //            await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
    //            {
    //                //重複確認のキー
    //                UpdateByProperties = new List<string> { nameof(AssetEntity.Path) },
    //                ////更新対象
    //                //PropertiesToInclude = new List<string> {
    //                //    nameof(AssetEntity.Name),
    //                //    nameof(AssetEntity.FileSize),
    //                //    nameof(AssetEntity.Type),
    //                //    nameof(AssetEntity.Path),
    //                //    nameof(AssetEntity.ParentPath),
    //                //    nameof(AssetEntity.CreationFileTime),
    //                //    nameof(AssetEntity.ModifiedTime),
    //                //    nameof(AssetEntity.CapturedTime),
    //                //    nameof(AssetEntity.AddedTime),
    //                //    nameof(AssetEntity.Width),
    //                //    nameof(AssetEntity.Height),
    //                //    nameof(AssetEntity.Rating),
    //                // }

    //                //update になる場合に更新しないカラム
    //                PropertiesToExcludeOnUpdate = new List<string>
    //                {
    //                    nameof(AssetEntity.AddedTime),
    //                    nameof(AssetEntity.Memo)
    //                }
    //            });

    //            //BulkInsertOrUpdateAsync では DB への反映まで終わっているので不要
    //            //await context.SaveChangesAsync();
    //        }
    //        finally
    //        {
    //            context.ChangeTracker.AutoDetectChangesEnabled = true;
    //        }
    //    }
    //}


    //WILL: 速度問題がある場合は、先に DB に既存のアセットを割り出してから、既存の物は該当の情報のみアップデートするほうが高速か検証する
    //現状はシンプルな全部一旦 AssetEntity 作って、既存のものは該当情報のみ、新規の物はそのままインサートする実装
    //public async Task BulkSetRateAsync(IEnumerable<FileSystemItemBase> items,int rate)
    //{
    //    //一旦 AssetEntity を作ってレートをセットする
    //    List<AssetEntity> assets = new();
    //    AssetEntity asset;
    //    foreach (var item in items)
    //    {
    //        asset = CreateAssetEntity(item);
    //        asset.Rating = rate;
    //        assets.Add(asset);
    //    }

    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        try
    //        {
    //            //トラッカーをオフにすると速度面で改良できる場合がある
    //            context.ChangeTracker.AutoDetectChangesEnabled = false;

    //            //登録済みの物はレートのみ更新する
    //            await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
    //            {
    //                //重複確認のキー
    //                UpdateByProperties = new List<string> { nameof(AssetEntity.Path), nameof(AssetEntity.LibraryId) },

    //                //複雑な複合インデックス（ライブラリ機能）導入後から、UpdateByProperties が何故か正常に効かなくなったため
    //                PropertiesToIncludeOnUpdate = new List<string>
    //                {
    //                    nameof(AssetEntity.Rating)
    //                }

    //            });
    //        }
    //        finally
    //        {
    //            context.ChangeTracker.AutoDetectChangesEnabled = true;
    //        }
    //    }
    //}


    //BulkInsertOrUpdateAsync では複雑なインデックスが対象の時にうまく動かない（PropertiesToIncludeOnUpdate で指定しても、全カラムが更新される）ため、アップデートとインサート処理を分ける
    public async Task BulkSetRateAsync(IEnumerable<FileSystemItemBase> items, int rate)
    {

        List<AssetEntity> assets = new();
        AssetEntity asset;
        foreach (var item in items)
        {
            asset = CreateAssetEntity(item);
            asset.Rating = rate;
            assets.Add(asset);

        }
        //検索が早いハッシュセットに
        HashSet<PathLibPair> assetPathLibPairs = assets.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        try
        {
            //トラッカーをオフにすると速度面で改良できる場合がある
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
            List<AssetEntity>? existsAsset = await context.Assets.AsNoTracking().Where(a => relativePaths.Contains(a.Path)).ToListAsync();
            existsAsset = existsAsset.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();

            HashSet<PathLibPair> existPathLibPair = existsAsset.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();

            List<AssetEntity> newAssets = assets.Where(a => !existPathLibPair.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();


            if (existsAsset.Any())
            {
                foreach(AssetEntity target in existsAsset)
                {
                    target.Rating = rate;
                }
                await context.BulkUpdateAsync(existsAsset, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path), nameof(AssetEntity.LibraryId) },
                    PropertiesToIncludeOnUpdate = new List<string>
                    {
                        nameof(AssetEntity.Rating)
                    }
                });
            }
            if (newAssets.Any())
            {
                await context.BulkInsertAsync(newAssets);
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        
    }

    //public async Task SetMemoAsync(IEnumerable<FileSystemItemBase> items,string memo)
    //{

    //    List<AssetEntity> assets = new();
    //    AssetEntity asset;
    //    foreach(var item in items)
    //    {
    //        asset = CreateAssetEntity(item);
    //        asset.Memo = memo;
    //        assets.Add(asset);
    //    }

    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        try
    //        {
    //            //トラッカーをオフにすると速度面で改良できる場合がある
    //            context.ChangeTracker.AutoDetectChangesEnabled = false;

    //            //登録済みの物はレートのみ更新する
    //            await context.BulkInsertOrUpdateAsync(assets, new BulkConfig
    //            {
    //                //重複確認のキー
    //                UpdateByProperties = new List<string> { nameof(AssetEntity.Path), nameof(AssetEntity.LibraryId) },

    //                PropertiesToIncludeOnUpdate = new List<string>
    //                {
    //                    nameof(AssetEntity.Memo)
    //                }
    //            });
    //        }
    //        finally
    //        {
    //            context.ChangeTracker.AutoDetectChangesEnabled = true;
    //        }
    //    }
    //}


    //BulkInsertOrUpdateAsync では複雑なインデックスが対象の時にうまく動かない（PropertiesToIncludeOnUpdate で指定しても、全カラムが更新される）ため、アップデートとインサート処理を分ける
    public async Task SetMemoAsync(IEnumerable<FileSystemItemBase> items, string memo)
    {

        List<AssetEntity> assets = new();
        AssetEntity asset;
        foreach (var item in items)
        {
            asset = CreateAssetEntity(item);
            asset.Memo = memo;
            assets.Add(asset);

        }
        //検索が早いハッシュセットに
        HashSet<PathLibPair> assetPathLibPairs = assets.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        try
        {
            //トラッカーをオフにすると速度面で改良できる場合がある
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
            List<AssetEntity>? existsAsset = await context.Assets.Where(a => relativePaths.Contains(a.Path)).ToListAsync();
            existsAsset = existsAsset.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();

            HashSet<PathLibPair> existPathLibPair = existsAsset.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();

            List<AssetEntity> newAssets = assets.Where(a => !existPathLibPair.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();


            if (existsAsset.Any())
            {
                foreach (AssetEntity target in existsAsset)
                {
                    target.Memo = memo;
                }
                await context.BulkUpdateAsync(existsAsset, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path), nameof(AssetEntity.LibraryId) },
                    PropertiesToIncludeOnUpdate = new List<string>
                    {
                        nameof(AssetEntity.Rating)
                    }
                });
            }
            if (newAssets.Any())
            {
                await context.BulkInsertAsync(newAssets);
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        
    }

    public async Task<LibraryModel?> CreateLibraryAsync(string libName,string libPath)
    {
        //string libName = targetLib.Name;
        //string libPath = targetLib.RootPath;

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        using var trx = await context.Database.BeginTransactionAsync();

        // 登録する Path については、\,/ を末尾にあれば削除してから末尾に追加。
        // 親子判定の際の Path 比較において、A/FolderB と A/FolderBC/D のような場合に前方一致してしまわないように、
        // A/FrolderB/ と A/FolderBC/D/ とする
        char sepChar = DirectorySeparatorChar;
        LibraryEntity libraryEntity = CreateLibraryEntity(libName, libPath.TrimEnd(sepChar, AltDirectorySeparatorChar) + sepChar);

        //既存のライブラリの中に新しいライブラリを作っていないか確認していく

        //RootPath 被りは許されないので、被ってたらそのまま終了
        foreach(LibraryEntity lib in _library)
        {
            if(libraryEntity.RootPath == lib.RootPath)
            {
                return null;
            }
        }

        //ライブラリの ID を確定させる。既存と同じライブラリの Path が来ている場合は弾いているので、一意制約違反は無い想定
        context.Library.Add(libraryEntity);
        await context.SaveChangesAsync();

        LibraryEntity? parentLibraryEntity = await GetParentLibraryAsync(libraryEntity);


        //親ライブラリが無い場合、Assets テーブルから素の Path の変換を実施していく
        if (parentLibraryEntity == null || parentLibraryEntity.Id == OutOfLibraryId)
        {
            //Path にライブラリを含むアセットについて、相対パスに変換していく
            List<AssetEntity> targetAssets = await context.Assets.Where(a => a.Path.StartsWith(libraryEntity.RootPath)).ToListAsync();

            //既存のアセットがライブラリ内の場合に、パスを変更する
            foreach (AssetEntity asset in targetAssets)
            {
                asset.Path = asset.Path;
                asset.ParentPath = FormatDirectoryPath(asset.ParentPath);

                //Replace の第一引数は "" を禁止しているため
                if (libraryEntity.RootPath != "")
                {
                    asset.Path = asset.Path.Replace(libraryEntity.RootPath, "");
                    asset.ParentPath = FormatDirectoryPath(asset.ParentPath.Replace(libraryEntity.RootPath, ""));
                }

                asset.LibraryId = libraryEntity.Id;
            }

            await context.SaveChangesAsync();
        }
        else
        {
            //親ライブラリがある場合、自分の領域のアセットだけ奪う
            //パスの差分取得
            string diffPath = libraryEntity.RootPath.Replace(parentLibraryEntity.RootPath,"");

            //アセットテーブルから、親ライブラリかつ、パスの差分で Path が始まるアセットを取得
            List<AssetEntity> targetAssets = await context.Assets.Where(a => a.LibraryId == parentLibraryEntity.Id).Where(a => a.Path.StartsWith(diffPath)).ToListAsync();

            foreach(AssetEntity asset in targetAssets)
            {
                asset.Path = asset.Path.Replace(diffPath, "");
                asset.ParentPath = FormatDirectoryPath(asset.ParentPath.Replace(diffPath, ""));
                asset.LibraryId = libraryEntity.Id;
            }

            await context.SaveChangesAsync();
                    
        }

        await trx.CommitAsync();
        AddLibrary(libraryEntity);
        return new LibraryModel(libraryEntity.Name,libraryEntity.RootPath);

        
    }

    /* *************** 変更 ************** */

    public async Task<Result<LibraryModel>> UpdateLibraryAsync(string libName, string libPath)
    {
        //string libName = targetLib.Name;
        //string libPath = targetLib.RootPath;

        using var scope = _serviceProvider.CreateScope();
       
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        using var trx = await context.Database.BeginTransactionAsync();

        //まずは変更前のライブラリを取得
        LibraryEntity? libraryEntity = context.Library.Where(l => l.Name == libName).FirstOrDefault();

        if(libraryEntity == null)
        {
            //対象が存在しないため処理もしない
            return new Result<LibraryModel>(ResultCode.TargetNotFound,null);
        }

        //ライブラリのルートパスを変更
        libraryEntity.RootPath = FormatDirectoryPath(libPath);

        //既存のライブラリの中に新しいライブラリを作っていないか確認する

        //RootPath 被りは許されないので、被ってたらそのまま終了
        foreach (LibraryEntity lib in _library)
        {
            if (libraryEntity.RootPath == lib.RootPath)
            {
                return new Result<LibraryModel>(ResultCode.AlreadyExists,null);
            }
        }

        LibraryEntity parentLibraryEntity = await GetParentLibraryAsync(libraryEntity);

        //親と対象のライブラリのルートパス差分取得。
        string diffPath;
        if(parentLibraryEntity.Id != OutOfLibraryId)
        {
            diffPath = libraryEntity.RootPath.Replace(parentLibraryEntity.RootPath, "");
        }
        else
        {
            diffPath = libraryEntity.RootPath;
        }
            
        //アセットテーブルから、親ライブラリかつ、パスの差分で Path が始まるアセットを取得 -> libraryEntity のライブラリに引っ越す必要があるアセットを取得
        //これはライブラリフォルダの移動～ライブラリルートパスを修正するまでの間に、ライブラリ内のアセットにタグ付け等何かしらの操作をユーザが実施していた場合に発生する。

        //親ライブラリに所属しており、今回ルートパスを修正したライブラリに所属が変更されるアセットを取得
        List<AssetEntity> targetAssets = await context.Assets.AsNoTracking().Where(a => a.LibraryId == parentLibraryEntity.Id).Where(a => a.Path.StartsWith(diffPath)).ToListAsync();

        foreach(AssetEntity asset in targetAssets)
        {
            asset.Path = asset.Path.Replace(diffPath, "");
            asset.ParentPath = FormatDirectoryPath(asset.ParentPath.Replace(diffPath, ""));
            asset.LibraryId = libraryEntity.Id;
        }

        //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する

        //検索用に、Path 及び PathLibPair のハッシュセット作成
        HashSet<string> targetAssetsPathList = targetAssets.Select(a => a.Path).ToHashSet();
        HashSet<PathLibPair> targetAssetsPathLibPairList = targetAssets.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();

        //統合処理が必要になる可能性がある、targetAssets のいずれかのアセットとアセットパス・ライブラリ ID が一致するアセット群（こちらがもともとあるべきライブラリに所属しているアセットとして登録されていた方の行）
        List<AssetEntity> migrationCandidatesList = await context.Assets.AsNoTracking().Where(a => targetAssetsPathList.Contains(a.Path)).ToListAsync();
        migrationCandidatesList = migrationCandidatesList.Where(a => targetAssetsPathLibPairList.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();
        Dictionary<PathLibPair,AssetEntity> migrationCandidatesDict = migrationCandidatesList.ToDictionary(a => new PathLibPair(a.Path,a.LibraryId), a => a);

        //タグ統合の際に必要となる、統合対象のアセットに紐づいている、AssetTagPair のリスト
        HashSet<int> migrationCandidateIds = migrationCandidatesList.Select(a => a.Id).ToHashSet();
        List<AssetTagPairEntity> migrationCandidateAssetTagPairList = await context.AssetTagPairs.AsNoTracking().Where(p => migrationCandidateIds.Contains(p.AssetId)).ToListAsync();
        ILookup<int,AssetTagPairEntity> migrationCandidateAssetTagPairLookup = migrationCandidateAssetTagPairList.ToLookup(p => p.AssetId);

        //同じく統合時に利用する、targetAssets のアセットに紐づいている、AssetTagPair のリスト
        HashSet<int> targetAssetIds = targetAssets.Select(a => a.Id).ToHashSet();
        List<AssetTagPairEntity> targetAssetTagPairList = await context.AssetTagPairs.AsNoTracking().Where(p => targetAssetIds.Contains(p.AssetId)).ToListAsync();
        ILookup<int, AssetTagPairEntity> targetAssetTagPairLookup = targetAssetTagPairList.ToLookup(p => p.AssetId);

        //紐づけるタグペア―のリスト。ループ終了後にまとめて追加する
        List<AssetTagPairEntity> migrationAssetTagPairList = new();
        //削除するアセットのリスト。統合後に消される方。フォルダ移動～ルートパス修正までの操作でできてしまった方の Asset テーブルの行
        List<AssetEntity> deleteAssetList = new();

        List<AssetEntity> updateExistsAssetInLibrary = new();
        bool existsAssetInLibraryChangeFlag = false;

        foreach (AssetEntity asset in targetAssets)
        {
            existsAssetInLibraryChangeFlag = false;
            //ライブラリ移動前にライブラリ内に何かが登録されていた場合、統合処理を実施後、ライブラリ移動前に登録されてしまっていた方の行を削除する必要がある。
            //AssetEntity? existsAssetInLibrary = context.Assets.Where(a => a.Path == asset.Path && a.LibraryId == asset.LibraryId).FirstOrDefault();
            PathLibPair key = new PathLibPair(asset.Path, asset.LibraryId);

            //リスト内に Path,LibraryId が一致するものがあれば、内容を統合する
            if(migrationCandidatesDict.TryGetValue(key, out var existsAssetInLibrary))
            {
                //ライブラリ内のアセット,ライブラリの移動先ですでに作られてしまったアセットそれぞれについて、アセットタグペアを取得
                //List<AssetTagPairEntity> existsAssetInLibraryTagPairs = await context.AssetTagPairs.Where(p => p.AssetId == existsAssetInLibrary.Id).ToListAsync();
                //List<AssetTagPairEntity> existsAssetInLibraryTagPairs = migrationCandidateAssetTagPairList.Where(p => p.AssetId == existsAssetInLibrary.Id).ToList();
                List<AssetTagPairEntity> existsAssetInLibraryTagPairs = migrationCandidateAssetTagPairLookup[existsAssetInLibrary.Id].ToList();

                //List<AssetTagPairEntity> assetTagPairs = await context.AssetTagPairs.Where(p => p.AssetId == asset.Id).ToListAsync();
                //List<AssetTagPairEntity> assetTagPairs = targetAssetTagPairList.Where(p => p.AssetId == asset.Id).ToList();
                List<AssetTagPairEntity> assetTagPairs = targetAssetTagPairLookup[asset.Id].ToList();

                //存在判定用
                HashSet<int> existsAssetInLibraryTagIds = existsAssetInLibraryTagPairs.Select(p => p.TagId).ToHashSet();

                foreach (AssetTagPairEntity pair in assetTagPairs)
                {
                    //ライブラリ内のアセットが持っていないタグがあれば、タグを移植（タグペアのアセット ID を、ライブラリ内の方に変更）
                    //※ライブラリの移動先にすでに作られてしまったアセット行は将来削除される
                    if (!existsAssetInLibraryTagIds.Contains(pair.TagId))
                    {
                        AssetTagPairEntity newPair = new AssetTagPairEntity();
                        newPair.TagId = pair.TagId;
                        newPair.AssetId = existsAssetInLibrary.Id;
                        //context.AssetTagPairs.Add(newPair);
                        migrationAssetTagPairList.Add(newPair);
                    }
                }

                //レートは高い方で維持
                if (existsAssetInLibrary.Rating < asset.Rating)
                {
                    existsAssetInLibrary.Rating = asset.Rating;
                    existsAssetInLibraryChangeFlag = true;
                }

                //メモは改行して追記する。一番失いたくない
                if (!asset.Memo.IsNullOrEmpty())
                {
                    existsAssetInLibrary.Memo += "\n" + asset.Memo;
                    existsAssetInLibraryChangeFlag = true;
                }

                if(existsAssetInLibraryChangeFlag == true)
                {
                    updateExistsAssetInLibrary.Add(existsAssetInLibrary);
                }

                //context.Assets.Remove(asset);
                //削除対象について、EFCore の追跡を切る。
                //DbUpdateConcurrencyException が下記 asset で発生するため。原因は BulkDelete （EFCore 外で削除）した後で Save しているため
                //var entry = context.Entry(asset);
                //entry.State = EntityState.Detached;
                deleteAssetList.Add(asset);
            }
        }

        HashSet<int> deleteAssetIds = deleteAssetList.Select(a => a.Id).ToHashSet();
        List<AssetEntity> updateTargetAssetsList = targetAssets.Where(a => !deleteAssetIds.Contains(a.Id)).ToList();

        if(updateTargetAssetsList.Count > 0)
        {
            await context.BulkUpdateAsync(updateTargetAssetsList);
        }

        if(updateExistsAssetInLibrary.Count > 0)
        {
            await context.BulkUpdateAsync(updateExistsAssetInLibrary);
        }

        if (migrationAssetTagPairList.Count > 0)
        {
            await context.BulkInsertAsync(migrationAssetTagPairList);
        }
        if(deleteAssetList.Count > 0)
        {
            await context.BulkDeleteAsync(deleteAssetList);
        }

        //ライブラリの変更反映があるので、消してはいけない。変更規模は微量なので Bulk にわざわざする必要も無い。
        await context.SaveChangesAsync();

        await trx.CommitAsync();
        UpdateLibrary(libraryEntity);
        return new Result<LibraryModel>(ResultCode.Success,new LibraryModel(libraryEntity.Name, libraryEntity.RootPath));
    }

    /* *************** 削除 ************** */

    //使用していない + ライブラリに未対応
    //public async Task UnregisterAssetAsync(FileSystemItemBase target)
    //{
    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        AssetEntity? asset = await context.Assets.FirstOrDefaultAsync(asset => asset.Path == target.Path);

    //        if (asset == null)
    //        {
    //            return;
    //        }

    //        context.Assets.Remove(asset);
    //        await context.SaveChangesAsync();
    //    }
    //}

    public async Task UnregisterTagAsync(string tagName)
    {
        using var scope = _serviceProvider.CreateScope();
        
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

    public async Task UnregisterLibraryAsync(LibraryModel targetLib)
    {
        string name = targetLib.Name;
        using var scope = _serviceProvider.CreateScope();
    
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        LibraryEntity? targetLibrary = await context.Library.FirstOrDefaultAsync(l => l.Name == name);

        //対象のライブラリが無いなら終了
        if (targetLibrary == null)
        {
            return;
        }

        var trx = await context.Database.BeginTransactionAsync();

        //ライブラリ内のアセットパスを戻す

        //対象のアセットを取得
        List<AssetEntity> targetAsset = await context.Assets.Where(a => a.LibraryId == targetLibrary.Id).ToListAsync();

        //削除対象のライブラリが他のライブラリの子か確認
        LibraryEntity? parentLibrary = await GetParentLibraryAsync(targetLibrary);

        //親ライブラリが無い場合、絶対パスに直す
        if(parentLibrary == null || parentLibrary.Id == OutOfLibraryId)
        {
            foreach(AssetEntity asset in targetAsset)
            {
                asset.Path = targetLibrary.RootPath + asset.Path;
                asset.ParentPath = FormatDirectoryPath(targetLibrary.RootPath + asset.ParentPath);
                asset.LibraryId = OutOfLibraryId;
            }
        }
        //親ライブラリがいる場合、そちらに引き渡す
        else
        {
            //パスの差分取得
            string diffPath = targetLibrary.RootPath;
            if(parentLibrary.RootPath != "")
            {
                diffPath = diffPath.Replace(parentLibrary.RootPath, "");
            }

            foreach (AssetEntity asset in targetAsset)
            {
                asset.Path = diffPath + asset.Path;
                asset.ParentPath = FormatDirectoryPath(diffPath + asset.ParentPath);
                asset.LibraryId = parentLibrary.Id;
            }
            await context.SaveChangesAsync();
        }


        //削除を実行
        context.Library.Remove(targetLibrary);
        await context.SaveChangesAsync();
        await trx.CommitAsync();
        DeleteLibrary(targetLibrary.Id);
    }

    /* *************** タグの紐づけ関係 ************** */


    //TODO: bulk 版だけで良い気もする。他のも
    //public async Task AttachTagToAssetAsync(FileSystemItemBase item, string tagName)
    //{
    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        AssetEntity? assetEntity = await context.Assets.FirstOrDefaultAsync(a => a.Path == item.Path);
    //        TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

    //        //登録が無いものへの紐づけ要請がきたら、登録してから紐づけ
    //        if (assetEntity == null)
    //        {
    //            assetEntity = CreateAssetEntity(item);
    //            context.Assets.Add(assetEntity);
    //        }
    //        if (tagsEntity == null)
    //        {
    //            tagsEntity = CreateTagEntity(tagName);
    //            context.Tags.Add(tagsEntity);
    //        }

    //        //新規登録をした場合には、一旦 DB に反映する。ID は自動生成なので、DB に反映するまで未確定かつ、アセットとタグのペアは ID で管理するので、未発行状態では困るため
    //        if (context.ChangeTracker.HasChanges())
    //        {
    //            await context.SaveChangesAsync();
    //        }

    //        //アセット - タグの組み合わせで重複がないか確認
    //        if (await context.AssetTagPairs.AnyAsync(a => a.AssetId == assetEntity.Id && a.TagId == tagsEntity.Id) == true)
    //        {
    //            return;
    //        }

    //        AssetTagPairEntity assetTagPairEntity = new AssetTagPairEntity();
    //        assetTagPairEntity.AssetId = assetEntity.Id;
    //        assetTagPairEntity.TagId = tagsEntity.Id;
    //        context.AssetTagPairs.Add(assetTagPairEntity);
    //        await context.SaveChangesAsync();
    //    }
    //}

    public async Task BulkAttachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        if(items.Count() == 0 || tagName.IsNullOrEmpty() == true)
        {
            return;
        }
        //受け取ったアセットのパスのリスト
        HashSet<string> assetPaths = items.Select(a => a.Path).ToHashSet();
        HashSet<PathLibPair> assetPathLibPairs = items.Select(a => ConvertPathToPathLibPair(a.Path)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            //DB から登録済みの物を取得
            //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
            List<AssetEntity>? assetEntity = await context.Assets.AsNoTracking().Where(a => relativePaths.Contains(a.Path)).ToListAsync();
            assetEntity = assetEntity.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();

            TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

            //InsertOrUpdate では SetOutputIdentity が機能しないため、新規登録分と既存分を識別して新規分のみ登録する
            //負荷が大きく変わらないと思うので、必要であれば全件 InsertOrUpdate (PropertiesToIncludeOnUpdate = new List<string> { "" } で update はしない指定)して、
            //その後対象をまとめて取得、とするとシンプル。おそらく現状の方がパフォーマンスは出てるはず

            //登録済みのパスをハッシュセット -> 差分を取って、DB に無いアセットのパスをハッシュセットに
            //Hashset 作成時に、パスは本来の絶対パス（LibRootPath + Path）に戻す。assetPath がライブラリを考慮しない絶対パスであるため
            //WILL: 速度に問題がある場合、どちらの方が良いか検証すること
            //HashSet<string> existsAssetPaths = assetEntity.Select(a => $"{a.Library?.RootPath}{a.Path}").ToHashSet();
            //HashSet<string> newAssetPaths = assetPaths.Where(path => !existsAssetPaths.Contains(path)).ToHashSet();

            HashSet<PathLibPair> existsPairs = assetEntity.Select(a => new PathLibPair(a.Path, a.LibraryId)).ToHashSet();
            List<FileSystemItemBase> newAssets = items.Where(a => !existsPairs.Contains(ConvertPathToPathLibPair(a.Path))).ToList();

            //登録が無いものへの紐づけ要請がきたら、登録して、assetEntity リストに追加
            if (newAssets.Any())
            {
                List<AssetEntity> newAssetEntity = newAssets.Select(a => CreateAssetEntity(a)).ToList();

                await context.BulkInsertAsync(newAssetEntity, new BulkConfig
                {
                    //重複確認のキー
                    UpdateByProperties = new List<string> { nameof(AssetEntity.Path), nameof(AssetEntity.LibraryId) },
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

    //public async Task DetachTagToAssetAsync(FileSystemItemBase item, string tagName)
    //{
    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        AssetEntity? assetEntity = await context.Assets.FirstOrDefaultAsync(a => a.Path == item.Path);
    //        TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);

    //        //念のため
    //        if (assetEntity == null || tagsEntity == null)
    //        {
    //            return;
    //        }

    //        await context.AssetTagPairs.Where(pair => pair.AssetId == assetEntity.Id && pair.TagId == tagsEntity.Id).ExecuteDeleteAsync();
    //        await context.SaveChangesAsync();
    //    }
    //}

    public async Task BulkDetachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        if (items.Count() == 0 || tagName.IsNullOrEmpty() == true)
        {
            return;
        }
        //受け取ったアセットのパスのリスト
        HashSet<string> assetPaths = items.Select(a => a.Path).ToHashSet();
        HashSet<PathLibPair> assetPathLibPairs = items.Select(a => ConvertPathToPathLibPair(a.Path)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            //tag の取得、tag が無いなら終了
            TagsEntity? tagsEntity = await context.Tags.FirstOrDefaultAsync(tag => tag.TagName == tagName);
            if(tagsEntity == null)
            {
                return;
            }

            //DB から登録済みの物を取得
            //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
            List<AssetEntity>? assetEntity = await context.Assets.AsNoTracking().Where(a => relativePaths.Contains(a.Path)).ToListAsync();
            assetEntity = assetEntity.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();
                

            //対象のアセットが DB に存在しないならタグ解除も自動的に必要が無い
            if (assetEntity.Any() == false)
            {
                return;
            }

            //削除する assetTagPairEntitys のリストの作成
            List<AssetTagPairEntity> assetTagPairEntitys = assetEntity.Select(asset => CreateAssetTagPairEntity(asset, tagsEntity)).ToList();

            await context.BulkDeleteAsync(assetTagPairEntitys, new BulkConfig
            {
                //重複確認のキー
                UpdateByProperties = new List<string> { nameof(AssetTagPairEntity.AssetId), nameof(AssetTagPairEntity.TagId)},
            });
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
    /* *************** 読み取り ************** */

    //全てのタグを取得
    public async Task<List<ItemTagModel>> GetAllTagsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        //List<string> tags = await context.Tags.Select(t => t.TagName).ToListAsync();
        List<ItemTagModel> tags = await context.Tags.AsNoTracking().Select(t => new ItemTagModel(t.TagName, t.Id)).ToListAsync();
        return tags;
    }

    //Asset から、紐づいているタグのリストを string で取得
    public async Task<List<string>> GetTagsFromAssetPathAsync(FileSystemItemBase item)
    {
        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        //まずは対象のアセットを取得
        PathLibPair pathLibPair = ConvertPathToPathLibPair(item.Path);
        int? libId = pathLibPair.GetLibraryId();
        AssetEntity? asset = await context.Assets.AsNoTracking().Where(a => a.Path == pathLibPair.Path).Where(a => a.LibraryId == libId).FirstOrDefaultAsync();
        if (asset == null)
        {
            return new List<string>();
        }

        //特定したアセットについて、対応する TagsEntity の TagName を取り出し、リストにして返却
        List<string> tags = await context.AssetTagPairs.Where(pair => pair.AssetId == asset.Id).Select(pair => pair.TagsEntity.TagName).AsNoTracking().ToListAsync();
        return tags;
    }



    //Asset の一覧から、全てに紐づいているタグのリストを string で取得
    public async Task<HashSet<string>> GetIntersectTagsFromAssetListAsync(IEnumerable<FileSystemItemBase> assets)
    {
        //検索が早いハッシュセットに
        HashSet<PathLibPair> assetPathLibPairs = assets.Select(a => ConvertPathToPathLibPair(a.Path)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();


        //まずは対象アセットの ID を取得する
        HashSet<int> assetIds = new();

        //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
        List<AssetEntity>? assetEntity = await context.Assets.Where(a => relativePaths.Contains(a.Path)).AsNoTracking().ToListAsync();
        assetEntity = assetEntity.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();

        assetIds = assetEntity.Select(a => a.Id).ToHashSet();

        //DB に登録されたアセットが無いか、１つでも未登録ならすべてに登録されたタグは無い
        if (assetIds.Count == 0 || assetIds.Count != assetPathLibPairs.Count)
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
            .AsNoTracking()
            .ToListAsync();

        HashSet<string> result = tags.ToHashSet();

        return result ?? new HashSet<string>();

    }

    // 文字列に対して、そのタグが存在するかを取得
    public async Task<bool> IsTagExistsAsync(string tagName)
    {
        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        return await context.Tags.AsNoTracking().AnyAsync(t => t.TagName == tagName);
    }


    public async Task<HashSet<string>> GetAssetPathsByAllTagsAndParentPathAsync(IEnumerable<ItemTagModel> tags,string parentPath)
    {
        if ((tags.Count() < 1 || tags == null) || parentPath == null) return new HashSet<string>();

        List<int> tagIds = tags.Select(tag => tag.TagId).ToList();
        //int tagCnt = tagIds.Count();

        using var scope = _serviceProvider.CreateScope();
        
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
        PathLibPair pathLibPair = ConvertDirectoryPathToPathLibPair(parentPath);

        int? libId = pathLibPair.GetLibraryId();
        var query = context.Assets.AsNoTracking().Include(a => a.Library)
            .Where(a => a.ParentPath == pathLibPair.Path).Where(a => a.LibraryId == libId);

        //WILL: 最適化が必要なら、タグに紐づいたアセットの件数を保持するように。もっと必要なら、ディレクトリごと等で保持する事も検討
        //どうしても最適化がもっといるなら、付与されてるアセットが少ないタグの順番で探索すると良い
        //が、タグが何件紐づいてるか別にテーブルが必要そう
        foreach (var id in tagIds)
        {
            // 各タグが「存在する」ことを条件として追加していく
            query = query.Where(a => a.AssetTagPairs.Any(pair => pair.TagId == id));
        }

        //List<string> result = await query.Select(a => a.Path).ToListAsync();

        // a => (a.Library?.RootPath ?? "") + a.Path とは書けないようなので下記のように記載
        return await query.Select(a => (a.LibraryId != null ? a.Library!.RootPath : "") + a.Path).ToHashSetAsync();
    }

    //選択中のアセットのレート取得
    public async Task<int> GetRateFromAssetsAsync(IEnumerable<FileSystemItemBase> assets)
    {
        HashSet<PathLibPair> assetPathLibPairs = assets.Select(a => ConvertPathToPathLibPair(a.Path)).ToHashSet();
        HashSet<string> relativePaths = assetPathLibPairs.Select(pair => pair.Path).ToHashSet();

        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        //ORM ではレコード型を解釈するのが難しいようなので、いったん解釈しやすい形で絞り込んでから、LibId が一致するかを C# の世界で検証する
        List<AssetEntity>? assetEntity = await context.Assets.AsNoTracking().Where(a => relativePaths.Contains(a.Path)).ToListAsync();
        assetEntity = assetEntity.Where(a => assetPathLibPairs.Contains(new PathLibPair(a.Path, a.LibraryId))).ToList();


        //Rate について取得後 Distinct で重複を排除
        List<int> rates = assetEntity.Select(a => a.Rating).Distinct().ToList();

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

    //あるディレクトリのアセットのレートを、パスとレートの辞書で返す
    public async Task<Dictionary<string,int>> GetRateFromParentPathAsync(IEnumerable<FileSystemItemBase> assets,string parentPath)
    {
        using var scope = _serviceProvider.CreateScope();
        
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        PathLibPair pathLibPair = ConvertDirectoryPathToPathLibPair(parentPath);
        int? libId = pathLibPair.GetLibraryId();

        return await context.Assets.AsNoTracking().Include(a => a.Library)
            .Where(a => a.ParentPath == pathLibPair.Path).Where(a => a.LibraryId == libId)
            .ToDictionaryAsync(a => (a.LibraryId != null ? a.Library!.RootPath : "") + a.Path, a => a.Rating);
    }


    //受け取った assets について、DB に登録されているものに対して、レートとタグを直接セット(ライブラリ未対応)
    //public async Task SetTagAndRateToFileSystemBaseModelAsync(IEnumerable<FileSystemItemBase> assets)
    //{
    //    HashSet<string> assetPashs = assets.Select(a => a.Path).ToHashSet();

    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    //        //Dictionary<string, AssetEntity> assetEntitys = await context.Assets.Where(a => assetPashs.Contains(a.Path)).ToDictionaryAsync(a => a.Path, a => a);

    //        ////まず AssetTagPairs から対象のアセットに関連する行に絞り込み -> アセット Path でグループ化 -> Path, tagList に select -> 辞書に変換
    //        //Dictionary<string, List<string>> assetTags = await context.AssetTagPairs
    //        //    .Where(pair => assetEntitys.ContainsKey(pair.AssetEntity.Path))
    //        //    .GroupBy(pair => pair.AssetEntity.Path)
    //        //    .Select(g => new { Path = g.Key, Tags = g.Select(pair => pair.TagsEntity.TagName).ToList() })
    //        //    .ToDictionaryAsync(t => t.Path, t => t.Tags);


    //        //foreach (FileSystemItemBase asset in assets)
    //        //{
    //        //    if(assetEntitys.TryGetValue(asset.Path, out AssetEntity? entity))
    //        //    {
    //        //        asset.Metadata.Rating = entity.Rating;
    //        //    }
    //        //    if(assetTags.TryGetValue(asset.Path,out List<string>? tags))
    //        //    {
    //        //        asset.Metadata.Tags = tags;
    //        //    }
    //        //}

    //        //受け取ったアセットリストと対応する行のみを asset テーブルから select -> 匿名型を作成、中身はアセットパス、レート、中間テーブルをたどって取得可能なタグ名のリスト -> パスを Key に指定、作成した匿名型自体を value にして、辞書を作成 
    //        var assetDatas = await context.Assets
    //            .Where(a => assetPashs.Contains(a.Path))
    //            .Select(a => new
    //            {
    //                Path = a.Path,
    //                Rating = a.Rating,
    //                Tags = a.AssetTagPairs.Select(pair => pair.TagsEntity.TagName).ToList()
    //            })
    //            .ToDictionaryAsync(a => a.Path, a => a);

    //        foreach(FileSystemItemBase asset in assets)
    //        {
    //            if(assetDatas.TryGetValue(asset.Path,out var Data))
    //            {
    //                asset.Metadata.Rating = Data.Rating;
    //                asset.Metadata.Tags = Data.Tags;
    //            }
    //        }
    //    }
    //}

    public async Task<string?> GetMemoAsync(FileSystemItemBase asset)
    {
        using var scope = _serviceProvider.CreateScope();
        MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        PathLibPair pathLibPair = ConvertPathToPathLibPair(asset.Path);
        int? libId = pathLibPair.GetLibraryId();

        AssetEntity? assetEntity = context.Assets.AsNoTracking().FirstOrDefault(a => a.Path == pathLibPair.Path && a.LibraryId == libId);

        if(assetEntity == null)
        {
            return null;
        }

        return assetEntity.Memo;

    }

    /* *************** その他 ************** */

    //ファイルパスとライブラリ ID をセットで扱うためのレコード
    public record struct PathLibPair
    {
        public string Path { get; set; }
        public int LibraryId { get; set; }
        //public PathLibPair(string path,LibraryEntity? library = null)
        //{
        //    Path = path;
        //    LibraryId = library?.Id ?? null;
        //}

        public PathLibPair(string path, int libraryId = OutOfLibraryId)
        {
            Path = path;
            LibraryId = libraryId;
        }

        public int? GetLibraryId()
        {
            return LibraryId;
        }

        public string GetInfo()
        {
            return $@"Path: {Path}, LibraryId: {LibraryId}";
        }
    }


    //ディレクトリのパスを ConvertPathToPathLibPair に入れるときは、この関数を噛ませること
    //ConvertPathToPathLibPair(FormatDirectoryPath(parentPath));
    public string FormatDirectoryPath(string path)
    {
        // 登録する Path については、\,/ を末尾にあれば削除してから末尾に追加。
        // 親子判定の際の Path 比較において、A/FolderB と A/FolderBC/D のような場合に前方一致してしまわないように、
        // A/FrolderB/ と A/FolderBC/D/ とする
        char sepChar = DirectorySeparatorChar;
        return path.TrimEnd(sepChar, AltDirectorySeparatorChar) + sepChar;

    }

    public PathLibPair ConvertPathToPathLibPair(string path)
    {
        //WILL: ワークロード的にあまりない気もするが、大量登録が重い場合には共通フォルダ内等専用の処理を作ってこの処理を減らす(同一フォルダ内専用の処理を作って、フォルダのパスについて処理を１回だけで済ませる)
        LibraryEntity? parentLibrary = null;
        string assetPath = path;
        foreach (LibraryEntity lib in _library)
        {
            //アセットがライブラリ内なら
            if (path.StartsWith(lib.RootPath, StringComparison.OrdinalIgnoreCase))
            {
                if (parentLibrary == null)
                {
                    parentLibrary = lib;
                }
                //複数親候補がいる場合、最もパスが長い = 最も直近の親が対象となる
                else if (parentLibrary.RootPath.Length < lib.RootPath.Length)
                {
                    parentLibrary = lib;
                }
            }
        }

        if(parentLibrary != null && parentLibrary.Id != OutOfLibraryId)
        {
            assetPath = assetPath.Replace(parentLibrary.RootPath, "");
        }

        return new PathLibPair(assetPath, parentLibrary?.Id ?? OutOfLibraryId);
    }
    public PathLibPair ConvertDirectoryPathToPathLibPair(string path)
    {
        PathLibPair pathLibPair = ConvertPathToPathLibPair(FormatDirectoryPath(path));
        pathLibPair.Path = FormatDirectoryPath(pathLibPair.Path);
        return pathLibPair;
    }

    //FileSystemItemBase から、Asset テーブルに書き込むための情報を取得格納
    public AssetEntity CreateAssetEntity(FileSystemItemBase target,bool recursiveFlag = false)
    {
        AssetEntity asset = new AssetEntity();

        //アイテム名
        asset.Name = target.Metadata.Name;

        //ファイルサイズ。ディレクトリなら格納しない
        if (target.Metadata.Length is long size)
        {
            asset.FileSize = size;
        }


        PathLibPair pair = ConvertPathToPathLibPair(target.Path);

        asset.Path = pair.Path;
        asset.LibraryId = pair.LibraryId;

        if (pair.LibraryId == OutOfLibraryId)
        {
            asset.ParentPath = System.IO.Path.GetDirectoryName(target.Path) ?? "";
        }
        else
        {
            string? libRootPath = _library.Where(l => l.Id == pair.LibraryId).Select(l => l.RootPath).FirstOrDefault();
            if(libRootPath == null)
            {
                //_logger.LogError("ライブラリ ID が存在するにも関わらず、対応するライブラリが _library にありませんでした。DB から取得を試行します。{LibId}", pair.LibraryId);

                //using var scope = _serviceProvider.CreateScope();
                //MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                //libRootPath = context.Library.Where(l => l.Id == pair.LibraryId).Select(l => l.RootPath).FirstOrDefault();
                //if(libRootPath == null)
                //{
                //    _logger.LogError("DB に存在しないライブラリ ID が _library に存在し、かつ RootPath が null でした。{LibId}", pair.LibraryId);
                //}

                string msg = "";
                msg += "ID,RootPath,Name";
                foreach (var l in _library)
                {
                    msg += l.Id + "," + l.RootPath + "," + l.Name;
                }

                if (recursiveFlag == true)
                {
                    _logger.LogError("データベースサービス再初期化後のリトライにおいても、問題が解消しませんでした。{LibId}\n{msg}", pair.LibraryId, msg);
                    //TODO: 理論上ここには到達しないはず。後ほど安全にメッセージ出しつつアプリを終了するなりの処理を実装する。
                    return null;

                }
                else
                {
                    _logger.LogError("ライブラリ ID が存在するにも関わらず、対応するライブラリが _library にありませんでした。不整合を解消するため、DB サービスを再初期化してリトライします。{LibId}\n{msg}", pair.LibraryId, msg);

                    Init();
                    return CreateAssetEntity(target, true);
                }
            }

            asset.ParentPath = System.IO.Path.GetDirectoryName(asset.Path) ?? "";

            if(libRootPath != null)
            {
                asset.ParentPath = asset.ParentPath.Replace(libRootPath, "");
            }
        }
        asset.ParentPath = FormatDirectoryPath(asset.ParentPath);

        asset.CreationFileTime = target.Metadata.CreationFileTime;
        asset.ModifiedTime = target.Metadata.ModifiedTime;
        asset.AddedTime = DateTimeOffset.Now;



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



    public LibraryEntity CreateLibraryEntity(string Name,string Path)
    {
        LibraryEntity lib = new();
        lib.Name = Name;
        lib.RootPath = Path;
        return lib;
    }


    public async Task<LibraryEntity> GetParentLibraryAsync(LibraryEntity lib)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            MyDbContext context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            //既存のライブラリの中に新しいライブラリを作っていないか確認する
            List<LibraryEntity> existsLibraryEntity = await context.Library.ToListAsync();

            //最低でも OutOfLibrary が親にいる。ライブラリとしてこれは絶対にある想定
            LibraryEntity parentLibraryEntity = OutOfLibraryEntity;
            foreach (LibraryEntity library in existsLibraryEntity)
            {
                //同じパスは自分自身なのでスルー
                if (library.RootPath == lib.RootPath)
                {
                    continue;
                }


                //大文字小文字は区別せずに、追加するライブラリのパスが既存のライブラリパスから始まっているか = 既存ライブラリの子であるかを判定する
                if (lib.RootPath.StartsWith(library.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    //if (parentLibraryEntity == null)
                    //{
                    //    parentLibraryEntity = library;
                    //}
                    ////複数親候補がいる場合、最もパスが長い = 最も直近の親が対象となる
                    //else 
                    //if (parentLibraryEntity.RootPath.Length < library.RootPath.Length)
                    //{
                    //    parentLibraryEntity = library;
                    //}

                    //最もルートパスが長いものが、最も直近の親のライブラリ
                    if (parentLibraryEntity.RootPath.Length < library.RootPath.Length)
                    {
                        parentLibraryEntity = library;
                    }
                }
            }
            return parentLibraryEntity;
        }
    }


}
