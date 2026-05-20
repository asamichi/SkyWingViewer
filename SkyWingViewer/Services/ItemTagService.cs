using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Utilities;
using SkyWingViewer.Models;
using SkyWingViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Navigation;

namespace SkyWingViewer.Services;

public class ItemTagService : ObservableObject
{
    private DatabaseService _databaseService;
    private ILogger _logger;

    public ItemTagService(DatabaseService databaseService, ILogger<ItemTagService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;

    }




    /* *************** 書き込み ************** */
    //public async Task CreateTagAsync(string tagName)
    //{
    //    await _databaseService.RegistrationTagAsync(tagName);
    //}

    
    //public async Task AttachTagToAssetAsync(FileSystemItemBase asset,string tagName)
    //{
    //    try
    //    {
    //        await _databaseService.AttachTagToAssetAsync(asset, tagName);
    //    }
    //    catch(Exception ex)
    //    {
    //        _logger.LogError("AttachTagToAssetAsync の際に例外が発生しました。{ex}", ex);
    //    }
    //}


    public async Task BulkAttachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        try
        {
            await _databaseService.BulkAttachTagToAssetAsync(items, tagName);
            items.ToList().ForEach(i => i.Metadata.Tags.Add(tagName));
        }
        catch (Exception ex)
        {
            _logger.LogError("BulkAttachTagToAssetAsync の際に例外が発生しました。{ex}", ex);
        }
    }

    /* *************** アセットとタグの紐づけ解除 ************** */

    //public async Task DetachTagToAssetAsync(FileSystemItemBase asset, string tagName)
    //{
    //    try
    //    {
    //        await _databaseService.DetachTagToAssetAsync(asset, tagName);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError("DetachTagToAssetAsync の際に例外が発生しました。{ex}", ex);
    //    }
    //}

    public async Task BulkDetachTagToAssetAsync(IEnumerable<FileSystemItemBase> items, string tagName)
    {
        try
        {
            await _databaseService.BulkDetachTagToAssetAsync(items, tagName);
            items.ToList().ForEach(i => i.Metadata.Tags.Remove(tagName));
         }
        catch (Exception ex)
        {
            _logger.LogError("BulkDetachTagToAssetAsync の際に例外が発生しました。{ex}", ex);
        }
    }

    /* *************** 削除 ************** */

    //TODO: タグ検索で毎回DBを見に行かなく改善する場合に、タグ削除直後の検索結果に削除されたはずのタグ情報が残るかも。そも消したタグは検索対象として選べないので大丈夫な気もするが確認はすること。エッジケース
    public async Task UnregisterTagAsync(string tagName)
    {
        try
        {
            await _databaseService.UnregisterTagAsync(tagName);
        }
        catch (Exception ex)
        {
            _logger.LogError("UnregisterTagAsync の際に例外が発生しました。{ex}", ex);
        }
    }


    /* *************** 読み取り ************** */

    public async Task<List<ItemTagModel>> GetAllTagsAsync()
    {
        try
        {
            List<ItemTagModel> tags = await _databaseService.GetAllTagsAsync();
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateTagList の際に例外が発生しました。{ex}", ex);
            //TODO: 例外を投げて、おかしかったと VM 層に伝える方が堅牢なはず
            return new List<ItemTagModel>();
        }
    }

    public async Task<List<string>> GetTagsFromAssetAsync(FileSystemItemBase asset)
    {
        return await _databaseService.GetTagsFromAssetPathAsync(asset);
    } 

    public async Task<HashSet<string>> GetIntersectTagsFromAssetListAsync(IEnumerable<FileSystemItemBase> assets)
    {
        try
        {
            return await _databaseService.GetIntersectTagsFromAssetListAsync(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetIntersectTagsFromAssetList の際に例外が発生しました。{ex}", ex);
            return new HashSet<string>();
        }
    }

    public async Task<bool> IsTagExists(string tagName)
    {
        try
        {
            return await _databaseService.IsTagExistsAsync(tagName);
        }
        catch (Exception ex)
        {
            _logger.LogError("IsTagExists の際に例外が発生しました。{ex}", ex);
            //利用され方的に、一旦あったということにしておく。無いと返して存在するタグの作成リクエストを飛ばしたくない。
            return true;
        }
    }

    public async Task<HashSet<string>> GetAssetPathsByAllTagsAndParentPathAsync(IEnumerable<ItemTagModel> tags, string parentPath)
    {
        try
        {
            return await _databaseService.GetAssetPathsByAllTagsAndParentPathAsync(tags, parentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAssetPathsByAllTagsAndParentPathAsync の際に例外が発生しました。{ex}", ex);
            return new HashSet<string>();
        }
    }

}
