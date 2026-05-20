using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SkyWingViewer.Services;

public partial class ItemSearchService : ObservableObject
{
    //ファイル名検索関連
    [ObservableProperty]
    public List<string> searchWords = new();
    public event Action? SearchWordsChanged;

    //タグ検索関連
    [ObservableProperty]
    public List<ItemTagModel> searchTags = new();
    public HashSet<string> targetAssetsByTag = new();
    public HashSet<string> searchTagNames = new();
    public event Action? SearchTagsChanged;


    [ObservableProperty]
    public int searchStarRate = 0;
    StarSearchModes starSearchMode = StarSearchModes.Minimum;
    enum StarSearchModes
    {
        Equal,
        Minimum,
        Maximum
    }
    public event Action? SearchStarRateChanged;



    private TargetNavigationService _targetNavigationService;
    private ItemTagService _itemTagService;
    private ILogger _logger;

    public ItemSearchService(TargetNavigationService targetNavigationService, ItemTagService itemTagService,ILogger<ItemSearchService> logger, StarRatingService starRatingService)
    {
        _logger = logger; ;
        _targetNavigationService = targetNavigationService;
        _itemTagService = itemTagService;


        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
    }

    public bool IsTarget(FileSystemItemBase item)
    {
        bool searchWordFlag = true;
        bool searchTagFlag = true;
        bool searchStarFlag = true;

        if(SearchWords.Count == 0) searchWordFlag = false;
        if(SearchTags.Count == 0) searchTagFlag = false;
        if((starSearchMode == StarSearchModes.Maximum && SearchStarRate == 5) || (starSearchMode == StarSearchModes.Minimum && SearchStarRate == 0)) searchStarFlag = false;


        //全ての検索が実施されないなら、 true で全て返す。
        if (!(searchWordFlag || searchTagFlag || searchStarFlag)) return true;

 
        bool isTarget = false;
        bool isTargetFromWord = true;
        bool isTargetFromTags = true;
        bool isTargetFromStar = true;

        // 原則　検索対象になる条件 == false{ flag = false} で記載する。

        if(searchWordFlag)
        {
            if( SearchWords.All(SearchWord => item.Metadata.Name.Contains(SearchWord, StringComparison.OrdinalIgnoreCase)) == false)
            {
                isTargetFromWord = false;
            }
        }

        if(searchTagFlag)
        {
            if(targetAssetsByTag.Contains(item.Path) == false)
            {
                isTargetFromTags = false;
            }
        }

        if (searchStarFlag)
        {
            //順番は使用される頻度が低そうな順で判定する
            if (starSearchMode == StarSearchModes.Minimum)
            {
                if((SearchStarRate <= item.Metadata.Rating) == false)
                {
                    isTargetFromStar = false;
                }
            }
            else if (starSearchMode == StarSearchModes.Equal)
            {
                if ((item.Metadata.Rating == SearchStarRate) == false)
                {
                    isTargetFromStar = false;
                }
            }
            else if (starSearchMode == StarSearchModes.Maximum)
            {
                if((item.Metadata.Rating <= SearchStarRate) == false)
                {
                    isTargetFromStar = false;

                }
            }
        }

        if(isTargetFromWord && isTargetFromTags && isTargetFromStar)
        {
            isTarget = true;
        }

        return isTarget;
    }

    //検索
    //ex) .Search<FileSystemItemViewModelBase>(assets,x=>x._model.Name.Contains("検索ワード");
    public IEnumerable<T> Search<T>(IList<T> list, Func<T, bool> SearchRequirements)
    {
        //return list.Where(SearchRequirements);
        foreach (T item in list)
        {
            if (SearchRequirements(item))
            {
                yield return item;
            }
        }
    }

    partial void OnSearchWordsChanged(List<string> value)
    {
        SearchWordsChanged?.Invoke();
    }

    partial void OnSearchTagsChanged(List<ItemTagModel> value)
    {
        _ = UpdateSearchTagsAndFire();
    }

    //処理順序を保証するためにどうしてもこうなる
    private async Task UpdateSearchTagsAndFire()
    {
        try
        {
            await GetTargetAssetsByTag();
            searchTagNames = SearchTags.Select(t => t.TagName).ToHashSet();
            SearchTagsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateSearchTagsAndFire の際に例外が発生しました。{ex}", ex);
        }
    }


    partial void OnSearchStarRateChanged(int value)
    {
        SearchStarRateChanged?.Invoke();
    }

    public void OnTargetPathChanged()
    {
        SearchWords = new();
        SearchTags = new();
        SearchStarRate = 0;
    }

    private async Task GetTargetAssetsByTag()
    {
        //今見ている対象（ターゲットパス）に存在する、該当タグを持っているアセットのリストをセットする。
        targetAssetsByTag = await _itemTagService.GetAssetPathsByAllTagsAndParentPathAsync(SearchTags, _targetNavigationService.Path);

    }
}
