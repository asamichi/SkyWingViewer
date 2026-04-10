using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class AssetListViewModelFactory
{
    //asset の形式に応じた VM を返却
    public static AssetViewModelBase? Create(AssetBase asset)
    {
        //WhenAddType: 対応形式が増えたらここに追加
        //asset の型で分岐
        switch (asset)
        {
            //ImageAsset 型なら、変数 image に入れつつ処理を進める（キャストを書かなくてよい）
            case ImageAsset image:
                return new ImageAssetViewModel(image);

            default:
                //TODO: 仮実装なので後程検討すること。
                return null;
        }
    }
}
