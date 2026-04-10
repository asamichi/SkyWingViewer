using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace SkyWingViewer.ViewModels;

//TODO: Bitmap を使用してサムネイルを作成して読み込むようにすること
//TODO: サムネイルは一定の規則に応じて保存して、サムネイルが無いかチェックする処理を追加すること
//TODO: 画像以外のものについては、対応するアイコンなどを用意して表示するようにする
//TODO: 非同期処理にしてファイル数が多いディレクトリでも操作感が悪くならないようにすること

public partial class AssetListViewModel : ObservableObject
{

    private TargetNavigationService _targetNavigationService;

    //ディレクトリ内の各アセットを格納
    public ObservableCollection<AssetViewModelBase> Assets { get; } = new();

    [ObservableProperty]
    public string? targetPath;

    public AssetListViewModel(TargetNavigationService targetNavigationService)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;
        LoadDirectory(TargetPath);
        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
    }



    //ディレクトリ内の各アセットを Assets コレクションに追加 = ListView の ItemsSource に追加
    public void LoadDirectory(string directoryPath)
    {
        //初期化
        Assets.Clear();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            var asset = AssetFactory.CreateAssetInstance(filePath);
            var vm = AssetListViewModelFactory.Create(asset);
            if (vm == null) continue;
            Assets.Add(vm);
        }
    }

    // TargetPath が変わった時の処理
    private void OnTargetPathChanged()
    {
        TargetPath = _targetNavigationService.Path;

        //新しいターゲットの内容に更新して表示する
        LoadDirectory(TargetPath);
    }

}
