using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using SkyWingViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;


//アセットリストから選択されているアセットの情報を保持するサービス
public partial class AssetSelectionService : ObservableObject
{
    //今選択されている対象
    [ObservableProperty]
    private List<FileSystemItemBase> targetItems = new List<FileSystemItemBase>();

    public event Action? TargetItemsChanged;

    public AssetSelectionService()
    {
    }


    partial void OnTargetItemsChanged(List<FileSystemItemBase> value)
    {
        TargetItemsChanged?.Invoke();
    }

}
