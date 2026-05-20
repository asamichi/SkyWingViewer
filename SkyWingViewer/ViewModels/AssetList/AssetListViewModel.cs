using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SkyWingViewer.ViewModels;

//TODO: 非同期処理にしてファイル数が多いディレクトリでも操作感が悪くならないようにすること

public partial class AssetListViewModel : ObservableObject
{

    //ディレクトリ内の各アセットを格納
    [ObservableProperty]
    private ObservableCollection<FileSystemItemViewModelBase> assets;

    public ListCollectionView AssetsView { get; set; }

    //VM のファクトリー
    public AssetListViewModelFactory _vmFactory;

    //キャンセルトークン
    public CancellationTokenSource? directoryCTS = null;

    //表示中のディレクトリ対象管理
    //現時点では TargetPath 自体は処理に使っていないが、ターゲットパス変更時にスクロールを一番上に戻すために必要
    private TargetNavigationService _targetNavigationService;
    [ObservableProperty]
    private string? targetPath;

    //詳細情報用のサービス等
    private ItemInformationService _itemInformationService;

    //表示対象のモデル管理
    private AssetListService _assetListService;

    //ListCollectionView の機能とどっちが良いか比較した結果、実装がシンプルなこちらを一旦採用
    private ItemSearchService _itemSearchService;

    //ソート。同上の理由でいったんこの方式で
    private ItemSortService _itemSortService;

    //選択中のアセットの管理
    private AssetSelectionService _assetSelectionService;

    private ILogger _logger;

    public AssetListViewModel(TargetNavigationService targetNavigationService,AssetListViewModelFactory factory, ItemInformationService itemInformationService, AssetListService assetListService,ItemSearchService itemSearchService,ItemSortService itemSortService,AssetSelectionService assetSelectionService,ILogger<AssetListViewModel> logger)
    {
        _targetNavigationService = targetNavigationService;
        TargetPath = _targetNavigationService.Path;
        _vmFactory = factory;
        _itemInformationService = itemInformationService;
        _assetListService = assetListService;
        _itemSearchService = itemSearchService;
        _itemSortService = itemSortService;
        _assetSelectionService = assetSelectionService;
        _logger = logger;
        Assets = new ObservableCollection<FileSystemItemViewModelBase>();
        AssetsView = new ListCollectionView(Assets);

        //LoadDirectory(TargetPath);
        OnTargetPathChanged();
        //イベント登録
        //_targetNavigationService.TargetPathChanged += OnTargetPathChanged;
        _assetListService.TargetPathChanged += OnTargetPathChanged;
        _assetListService.ItemsChanged += OnItemsChanged;
        _itemSortService.SortKeyChanged += OnSortKeyChanged;
    }



    //ディレクトリ内の各アセットを Assets コレクションに追加 = ListView の ItemsSource に追加
    public async Task LoadAssetsAsync()
    {
        Assets.Clear();
        directoryCTS?.Cancel();
        directoryCTS = new();
        CancellationToken token = directoryCTS.Token;

        //TODO: async XXX と別メソッドに切り出して、await XXX(); で呼ぶ方が読みやすいかも
        try
        {
            await Task.Run(async () =>
            {
                List<FileSystemItemViewModelBase> buffer = new List<FileSystemItemViewModelBase>();
                //見た目の気持ちよさとディレクトリ移動直後等のレスポンスの良さを考え、始めは 1 件ずつ表示。->ソート機能との兼ね合いで始めからそこそこのまとまりで読み込む。
                //見えないところまでいったら、効率化のためバッチサイズを大きくしてまとめて追加していくようにする。
                int BatchSize = 10;
                int TotalCnt = 0;

                foreach(var items in _assetListService.EnumerateLoadDirectory())
                {
                    if (token.IsCancellationRequested) break;

                    var viewModel = _vmFactory.Create(items, directoryCTS);

                    if (viewModel != null)
                    {
                        buffer.Add(viewModel);
                    }

                    if(buffer.Count > BatchSize)
                    {
                        await PushToAssetViewModelList(buffer, token);
                        buffer.Clear();
                    }
                    TotalCnt++;

                    if(TotalCnt > 100)
                    {
                        BatchSize = 100;
                    }
                }
                //まとめて処理する分の端数分
                if (buffer.Count > 0 && !token.IsCancellationRequested)
                {
                    await PushToAssetViewModelList(buffer, token);
                }

            }, token);
        }
        catch (OperationCanceledException) {
            //キャンセル時は何もしない
        }
    }

    private async Task PushToAssetViewModelList(List<FileSystemItemViewModelBase> list,CancellationToken token)
    {
        if (list == null) return;
        // リストのコピーを渡して、バックグラウンド側がすぐ次へ行けるようにする(LoadAssetsAsync では Clear してすぐ次に移りたい)
        var copy = list.ToList();

        //TODO: 実際の効果のほどはわかってない
        //指定されているソートでリストを渡すことで、対象が多いフォルダに移動した時のソートによる入れかえが見えにくいように
        //bool isAscending = _itemSortService.SortKey.SortDescription.Direction == ListSortDirection.Ascending;

        //if (isAscending)
        //{
        //    copy = copy.OrderBy(vm => _itemSortService.SortKey.GetSortTarget(vm.Model)).ToList();
        //}
        //else
        //{
        //    copy = copy.OrderByDescending(vm => _itemSortService.SortKey.GetSortTarget(vm.Model)).ToList();

        //}

        //UI スレッドで実行する必要がある
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            if (token.IsCancellationRequested) return;

            foreach (var item in copy)
            {
                Assets.Add(item);
            }
        });
    }

    // TargetPath が変わった時の処理
    private void OnTargetPathChanged()
    {
        TargetPath = _targetNavigationService.Path;

        //新しいターゲットの内容に更新して表示する
        _ = CallLoadAssetsAsync();
    }

    private async Task CallLoadAssetsAsync()
    {
        try
        {
            await LoadAssetsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("CallLoadAssetsAsync の中で例外が発生しました。{ex}", ex);
        }
    }

    //検索対象が変わったら読み込みしなおす
    private void OnItemsChanged()
    {
        AssetsView.Filter = (obj =>
        {
            if (obj is FileSystemItemViewModelBase vm)
            {
                return _itemSearchService.IsTarget(vm.Model);
            }
            return true;
        });

        AssetsView.Refresh();
    }

    //ソートキーが変わったら、ソートする
    private void OnSortKeyChanged()
    {
        AssetsView.SortDescriptions.Clear();

        AssetsView.SortDescriptions.Add(_itemSortService.SortKey.SortDescription);
    }

    //選択対象に応じた詳細を表示するよう、詳細表示サービスに対象を伝える
    //TODO: 選択対象を取得するサービスが２つ以上になったので、選択中の対象を管理するサービスを作って、皆底を参照するようにしても良いかも。
    [RelayCommand]
    public void UpdateSelection(object parameter)
    {

        //TODO: 詳細情報表示時点では AssetSelectionService が無かった。一旦追記したのみにしたが、余裕が出たら統合すること。
        List<IItemInformationProvider>? list = null;
        if (parameter is System.Collections.IList parameterList)
        {
            list = parameterList.OfType<IItemInformationProvider>().ToList();


            //選択されている対象のモデルを AssetSelectionService に反映
            List<FileSystemItemBase> selectedAssetList = new List<FileSystemItemBase>();
            foreach(var item in parameterList)
            {
                if(item is FileSystemItemViewModelBase vm)
                {
                    selectedAssetList.Add(vm.Model);
                }
            }
            _assetSelectionService.TargetItems = selectedAssetList;

        }
        _itemInformationService.TargetItems = list;

    }

}
