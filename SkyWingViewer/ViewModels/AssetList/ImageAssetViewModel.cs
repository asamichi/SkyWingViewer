using System;
using System.Collections.Generic;
using System.Text;

using SkyWingViewer.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using SkyWingViewer.Services;

using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SkyWingViewer.ViewModels;

public partial class ImageAssetViewModel : AssetViewModelBase<ImageAsset>
{

    //このインスタンスをデータコンテキストとしている View の数
    //折り返し部分などで、自分にとっては _lastVM だが、別の View が今まさに表示しているという場合があることへの対策。 == 0 の時のみサムネイルを解放

    public int ViewCount { get; set; } = 0;

    [ObservableProperty]
    private BitmapSource? thumbnail;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private int _isVisible = 0;
    private ThumbnailService thumbnailService;
    private ILogger _logger;

    public CancellationToken _cancellationToken;
    public CancellationTokenSource _childCancellationTokenSource;

    public IWindowService _windowService;
    public AssetListService _assetListService;

    private IServiceProvider _serviceProvider;

    public ImageAssetViewModel(ImageAsset imageAsset,ThumbnailService ts,ILogger<ImageAssetViewModel> logger,CancellationToken ct,IWindowService windowService,AssetListService assetListService, IServiceProvider serviceProvider) : base(imageAsset)
    {
        _serviceProvider = serviceProvider;
        thumbnailService = ts;
        _logger = logger;
        _cancellationToken = ct;

        _windowService = windowService;
        _assetListService = assetListService;

        ContextMenuItems.Add(new ContextMenuItem("内蔵画像ビューワで表示", OpenImageViewerCommand));
    }

    [RelayCommand]
    public void OpenImageViewer()
    {
        //ImageViewerMainViewModel viewerVM = new ImageViewerMainViewModel(_assetListService._assetModels,Model);
        ImageViewerMainViewModel viewerVM = ActivatorUtilities.CreateInstance<ImageViewerMainViewModel>(_serviceProvider, _assetListService._assetModels, Model);

        WindowServiceOptions options = new();
        //TODO: 前回ビューワ利用時の、最終サイズを保持してそれを維持するように。
        options.SizeToContent = SizeToContent.Manual;
        options.Width = 800;
        options.Height = 600;


        _windowService.CreateWindow(viewerVM,options);
    }


    public async Task LoadThumbnail()
    {
        try
        {
            _isVisible = 1;
            // すでに読み込み済みなら何もしない
            if (_isLoading == 1 || Thumbnail != null)
            {
                _logger.LogTrace("再度サムネイル作成要求がありました。。Path: {Path}", _asset.AssetPath);

                return;
            }

            _isLoading = 1;
            //Thumbnail = await Task.Run(() => ts.getImageCache(_asset.AssetPath));
            //Thumbnail = ts.getImageCache(_asset.AssetPath);


            _childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            _logger.LogTrace("サムネイルの作成リクエストを実施します。Path: {Path}", _asset.AssetPath);
            ThumbnailRequest thumbnailRequest = new ThumbnailRequest(_asset, async (result) =>
            {
                if (this._isVisible == 0)
                {
                    _logger.LogTrace("既に必要のないサムネイルのため、値を格納しません。Path: {Path}", _asset.AssetPath);
                    return;
                }
                this.Thumbnail = result;
            }, _childCancellationTokenSource.Token);
            await thumbnailService.AddQueueAsync(thumbnailRequest);

            _isLoading = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("LoadThumbnail の中で例外が発生しました。{ex}", ex);
        }

    }

    //TODO: 本当はVM側でキャンセルトークンソースを受け取って、子トークンをサムネイルサービスに渡してそれをキャンセルする仕組みにするべき
    public void UnloadThumbnail()
    {

        _logger.LogTrace("サムネイルを解放します。Path: {Path}", _asset.AssetPath);
        _isVisible = 0;
        Thumbnail = null;

        _childCancellationTokenSource?.Cancel();
        _childCancellationTokenSource?.Dispose();
    }


    //イベント発火テスト用
    //public async void test1()
    //{
    //    _logger.LogInformation("OnIsVisibleChanged!{path}", _asset.AssetPath);
    //}
    //public async void test2()
    //{
    //    _logger.LogInformation("OnLoaded!{path}", _asset.AssetPath);
    //}
}

