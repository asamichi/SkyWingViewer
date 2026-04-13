using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyWingViewer.ViewModels;


//それぞれのアセットタイプの ViewModel クラスの基底クラス
public abstract class AssetViewModelBase : ObservableObject
{
    protected readonly AssetBase _asset;
    public string AssetPath => _asset.AssetPath;
    protected AssetViewModelBase(AssetBase asset)
    {
        _asset = asset;
    }
}
