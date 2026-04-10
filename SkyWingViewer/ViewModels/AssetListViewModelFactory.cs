using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class AssetListViewModelFactory
{
    //asset の形式に応じた VM を返却
    public static object Create(AssetBase asset)
    {
        //WhenAddType: 他王形式が増えたらここに追加
        //asset の型で分岐
        switch (asset)
        {
            //ImageAsset 型なら、変数 image に入れつつ処理を進める（キャストを書かなくてよい）
            case ImageAsset image:
                return new ImageAssetViewModel(image);

            default:
                return null;
        }
    }
}
