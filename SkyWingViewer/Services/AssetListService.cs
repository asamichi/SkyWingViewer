using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Data;

namespace SkyWingViewer.Services;

public class AssetListService
{
    public event Action? TargetPathChanged;
    public event Action? ItemsChanged;

    private bool _isLoadedMetadataFromDB = false;

    //ディレクトリ内の各アセットを格納
    public List<FileSystemItemBase> _assetModels = new();
    public List<FileSystemItemBase> _directoryModels = new();
    public IEnumerable<FileSystemItemBase> _allModels => _assetModels.Concat(_directoryModels);

    //表示中のディレクトリ対象管理
    private TargetNavigationService _targetNavigationService;
    public string TargetPath { get; set; }

    private ItemSearchService _itemSearchService;
    private StarRatingService _starRatingService;
    private ILogger _logger;

    //TODO: せっかくアセットのリスト持っているので、一緒にタグとかの情報も持ってしまえば、検索条件変更したときにデータベースにアクセスしなくても良くできそう？

    public AssetListService(TargetNavigationService targetNavigationService,ItemSearchService itemSearchService,ILogger<AssetListService> logger,StarRatingService starRatingService)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;

        _itemSearchService = itemSearchService;
        _starRatingService = starRatingService;
        _logger = logger;
        
        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
        _itemSearchService.SearchWordsChanged += OnSearchWordsChanged;
        _itemSearchService.SearchTagsChanged += OnSearchTagsChanged;
        _itemSearchService.SearchStarRateChanged += OnSearchStarRateChanged;
    }



    public IEnumerable<FileSystemItemBase> EnumerateLoadDirectory()
    {

        string directoryPath = TargetPath;
        //初期化
        _assetModels.Clear();
        _directoryModels.Clear();


        //TODO: ソート時のちらつきについて、先にファイルパスとメタデータのみ生成して、ソート後に諸々処理する方法を試す。これなら軽いかも
        foreach (var directorys in Directory.EnumerateDirectories(directoryPath))
        {
            DirectoryModel directory = new DirectoryModel(directorys);
            _directoryModels.Add(directory);

            //if(_itemSearchService.IsTarget(directory))
                yield return directory;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            var asset = AssetFactory.CreateAssetInstance(filePath);
            _assetModels.Add(asset);
            //if(_itemSearchService.IsTarget(asset))
                    yield return asset;
        }
    }



    // TargetPath が変わった時の処理
    private void OnTargetPathChanged()
    {
        TargetPath = _targetNavigationService.Path;
        TargetPathChanged?.Invoke();
        _isLoadedMetadataFromDB = false;
    }

    private void OnSearchWordsChanged()
    {
        ItemsChanged?.Invoke();
    }
    private void OnSearchTagsChanged()
    {
        ItemsChanged?.Invoke();
    }


    //TODO: テストの範囲だとバグらないけど、うまいことやればバグらせられそうな実装。アセット読み込み時に DB からロードしてしまって良さそう。
    private void OnSearchStarRateChanged()
    {
        if(_isLoadedMetadataFromDB == false)
        {
            _ = UpdateRatesInAssetList();
        }
        else
        {
            ItemsChanged?.Invoke();
        }
    }

    private async Task UpdateRatesInAssetList()
    {
        try
        {
            await _starRatingService.SetRateFromParentPathAsync(_allModels);
            _isLoadedMetadataFromDB = true;
            ItemsChanged?.Invoke();

        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateRatesInAssetList の際に例外が発生しました。{ex}", ex);
        }
    }
}
