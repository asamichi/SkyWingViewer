using System;
using System.Collections.Generic;
using System.Text;

using SkyWingViewer.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using SkyWingViewer.Services;

namespace SkyWingViewer.ViewModels;

public partial class ImageAssetViewModel : AssetViewModelBase
{

    [ObservableProperty]
    private BitmapSource? thumbnail;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private ThumbnailService thumbnailService;

      
    public ImageAssetViewModel(ImageAsset imageAsset,ThumbnailService ts) : base(imageAsset)
    {
        thumbnailService = ts;
    }



    public async Task LoadThumbnail(CancellationToken cancellationToken)
    {
        if(_isLoading == 1 || Thumbnail != null)
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
            });
            await thumbnailService.AddQueueAsync(thumbnailRequest,cancellationToken);
        }
        //現状必要ない
        //_isLoading = 0;
    }
}

