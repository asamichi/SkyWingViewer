using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class AssetListViewModelFactory
{
    private ThumbnailService _thumbnailService;
    public AssetListViewModelFactory(ThumbnailService ts)
    {
        _thumbnailService = ts;
    }
    //asset の形式に応じた VM を返却
    public AssetViewModelBase? Create(AssetBase asset)
    {
        //WhenAddType: 対応形式が増えたらここに追加
        //asset の型で分岐
        switch (asset)
        {
            //ImageAsset 型なら、変数 image に入れつつ処理を進める（キャストを書かなくてよい）
            case ImageAsset image:
                return new ImageAssetViewModel(image,_thumbnailService);

            default:
                //TODO: 仮実装なので後程検討すること。
                return null;
        }
    }
}
