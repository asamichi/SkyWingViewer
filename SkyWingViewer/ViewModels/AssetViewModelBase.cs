using System;
using System.Collections.Generic;
using System.Text;

using SkyWingViewer.Models;
namespace SkyWingViewer.ViewModels;


//それぞれのアセットタイプの ViewModel クラスの基底クラス
public abstract class AssetViewModelBase
{
    protected readonly AssetBase _asset;
    public string AssetPath => _asset.AssetPath;
    protected AssetViewModelBase(AssetBase asset)
    {
        _asset = asset;
    }
}
