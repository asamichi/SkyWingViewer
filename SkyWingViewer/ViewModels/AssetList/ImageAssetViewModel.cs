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

public partial class ImageAssetViewModel : AssetViewModelBase<ImageAsset>
{

    [ObservableProperty]
    private BitmapSource? thumbnail;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private int _isVisible = 0;
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


        _logger.LogTrace("サムネイルの作成リクエストを実施します。Path: {Path}", _asset.AssetPath);
        ThumbnailRequest thumbnailRequest = new ThumbnailRequest(_asset,async (result) =>
        {
            if(this._isVisible == 0)
            {
                _logger.LogTrace("既に必要のないサムネイルのため、値を格納しません。Path: {Path}", _asset.AssetPath);
                return;
            }
            this.Thumbnail = result;
        }, _cancellationToken);
        await thumbnailService.AddQueueAsync(thumbnailRequest);


        _isLoading = 0;
    }

    //TODO: 本当はVM側でキャンセルトークンソースを受け取って、子トークンをサムネイルサービスに渡してそれをキャンセルする仕組みにするべき
    public void UnloadThumbnail()
    {
        _logger.LogTrace("サムネイルを解放します。Path: {Path}", _asset.AssetPath);
        _isVisible = 0;
        Thumbnail = null;
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

