using Microsoft.Extensions.DependencyInjection;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class AssetListViewModelFactory
{
    private IServiceProvider _serviceProvider;

    public AssetListViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    //asset の形式に応じた VM を返却

    public FileSystemItemViewModelBase? Create(FileSystemItemBase item, CancellationTokenSource cts)
    {
        //WhenAddType: 対応形式が増えたらここに追加
        //item の型で分岐
        switch (item)
        {
            //ImageAsset 型なら、変数 image に入れつつ処理を進める（キャストを書かなくてよい）
            case ImageAsset image:
                //[ActivatorUtilities.CreateInstance メソッド (Microsoft.Extensions.DependencyInjection) | Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.extensions.dependencyinjection.activatorutilities.createinstance?view=net-8.0)
                return ActivatorUtilities.CreateInstance<ImageAssetViewModel>(_serviceProvider, image, cts.Token);
            case OtherAsset otherAsset:
                return ActivatorUtilities.CreateInstance<OtherAssetViewModel>(_serviceProvider, otherAsset, cts.Token);
            case DirectoryModel directory:
                return ActivatorUtilities.CreateInstance<DirectoryViewModel>(_serviceProvider, directory);
            default:
                return null;
        }
    }
}
