using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
namespace SkyWingViewer.ViewModels;


//それぞれのアセットタイプの ViewModel クラスの基底クラス
public abstract partial class AssetViewModelBase<TModel> : FileSystemItemViewModelBase<TModel> where TModel : AssetBase
{
    protected TModel _asset => _model;

    public string AssetPath => _asset.AssetPath;

    protected AssetViewModelBase(TModel asset):base(asset)
    {

    }

}
