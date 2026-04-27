using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkyWingViewer.ViewModels;

//TODO: 非同期処理にしてファイル数が多いディレクトリでも操作感が悪くならないようにすること

public partial class AssetListViewModel : ObservableObject
{

    //<Object> は多分回避できるはず。
    //ディレクトリ内の各アセットを格納
    [ObservableProperty]
    private ObservableCollection<Object> assets = new();

    //VM のファクトリー
    public AssetListViewModelFactory _vmFactory;

    //キャンセルトークン
    public CancellationTokenSource? directoryCTS = null;

    //表示中のディレクトリ対象管理
    private TargetNavigationService _targetNavigationService;
    [ObservableProperty]
    public string? targetPath;

    //詳細情報用のサービス等
    private ItemInformationService _itemInformationService;

    public AssetListViewModel(TargetNavigationService targetNavigationService,AssetListViewModelFactory factory, ItemInformationService itemInformationService)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;
        _vmFactory = factory;
        _itemInformationService = itemInformationService;
        LoadDirectory(TargetPath);
        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
    }


    //TODO: VM に書くのは少し微妙な内容な気もするが、切り出すべきかと言われると最小の処理しか今はしてない感じもする。将来太ってきたら別クラスへの切り出しを検討
    //TODO: ファイル数膨大なディレクトリを開いて問題ありそうなら非同期にする等検討
    //ディレクトリ内の各アセットを Assets コレクションに追加 = ListView の ItemsSource に追加
    public void LoadDirectory(string directoryPath)
    {
        //初期化
        //new しないと今の仕組みだと一番上までスクロールしない
        //Assets.Clear();
        Assets = new ObservableCollection<Object>();
        if (directoryCTS != null)
        {
            directoryCTS.Cancel();
            directoryCTS.Dispose();
        }

        //このディレクトリで利用する cst を作成
        //WILL: キャンセルトークンの管理が複雑になったり何か困ったら、CommunityToolkit のメッセンジャーの利用を検討
        directoryCTS = new();

        foreach (var directorys in Directory.EnumerateDirectories(directoryPath))
        {
            DirectoryModel model = new DirectoryModel(directorys);
            DirectoryViewModel? directoryViewModel = (DirectoryViewModel?)_vmFactory.Create(model, directoryCTS);
            if(directoryViewModel != null)
            {
                Assets.Add(directoryViewModel);
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            var asset = AssetFactory.CreateAssetInstance(filePath);
            var vm = _vmFactory.Create(asset, directoryCTS);
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

    //
    [RelayCommand]
    public void UpdateSelection(object parameter)
    {

        List<IItemInformationProvider>? list = null;
        if (parameter is System.Collections.IList parameterList)
        {
            list = parameterList.OfType<IItemInformationProvider>().ToList();
        }
        _itemInformationService.TargetItems = list;

    }

}
