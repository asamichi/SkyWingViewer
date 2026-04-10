using System;
using System.Collections.Generic;
using System.Text;

using SkyWingViewer.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyWingViewer.ViewModels;

public class ImageAssetViewModel : AssetViewModelBase
{
    public ImageAssetViewModel(ImageAsset imageAsset) : base(imageAsset)
    {
    }
}

