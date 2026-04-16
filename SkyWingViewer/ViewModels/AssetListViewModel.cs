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
    public ObservableCollection<Object> Assets { get; } = new();

    public AssetListViewModelFactory _vmFactory;

    public CancellationTokenSource? directoryCTS = null;

    [ObservableProperty]
    public string? targetPath;

    public AssetListViewModel(TargetNavigationService targetNavigationService,AssetListViewModelFactory factory)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;
        _vmFactory = factory;
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
        Assets.Clear();
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
            DirectoryViewModel directoryViewModel = new DirectoryViewModel(directorys);
            Assets.Add(directoryViewModel);
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

}
