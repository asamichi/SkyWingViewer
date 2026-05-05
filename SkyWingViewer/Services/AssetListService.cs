using CommunityToolkit.Mvvm.ComponentModel;
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


    //ディレクトリ内の各アセットを格納
    public List<FileSystemItemBase> _assetModels = new();
    public List<FileSystemItemBase> _directoryModels = new();

    //表示中のディレクトリ対象管理
    private TargetNavigationService _targetNavigationService;
    public string TargetPath { get; set; }

    public ItemSearchService _itemSearchService;


    public AssetListService(TargetNavigationService targetNavigationService,ItemSearchService itemSearchService)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;

        _itemSearchService = itemSearchService;
        
        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
        _itemSearchService.SearchWordsChanged += OnSearchWordsChanged;
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
    }

    private void OnSearchWordsChanged()
    {
        ItemsChanged?.Invoke();
    }

}
