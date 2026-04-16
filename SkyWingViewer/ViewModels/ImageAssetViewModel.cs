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

namespace SkyWingViewer.ViewModels;

public partial class ImageAssetViewModel : AssetViewModelBase
{

    [ObservableProperty]
    private BitmapSource? thumbnail;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private ThumbnailService thumbnailService;
    private ILogger _logger;

    public CancellationToken _cancellationToken;


    public ImageAssetViewModel(ImageAsset imageAsset,ThumbnailService ts,ILogger<ImageAssetViewModel> logger,CancellationToken ct) : base(imageAsset)
    {
        thumbnailService = ts;
        _logger = logger;
        _cancellationToken = ct;
    }



    public async Task LoadThumbnail()
    {
        // すでに読み込み済みなら何もしない
        if (Thumbnail != null) return;

        if (_isLoading == 1 || Thumbnail != null)
        {
            return;
        }

        _isLoading = 1;
        //Thumbnail = await Task.Run(() => ts.getImageCache(_asset.AssetPath));
        //Thumbnail = ts.getImageCache(_asset.AssetPath);

        //ImageAsset 型なのは確定だが、エディタに対して明示
        if (_asset is ImageAsset imageAsset)
        {
            ThumbnailRequest thumbnailRequest = new ThumbnailRequest(imageAsset,(result) =>
            {
                this.Thumbnail = result;
            }, _cancellationToken);
            await thumbnailService.AddQueueAsync(thumbnailRequest, _cancellationToken);
        }
        //現状必要ない
        //_isLoading = 0;
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

