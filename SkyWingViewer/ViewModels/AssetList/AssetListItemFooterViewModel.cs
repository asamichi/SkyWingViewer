using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

using SkyWingViewer.Models;
using System.IO;
namespace SkyWingViewer.ViewModels;

public partial class AssetListItemFooterViewModel : ObservableObject
{
    [ObservableProperty]
    private string targetName;

    public AssetListItemFooterViewModel (FileSystemItemBase item)
    {
        //フォルダ名もこれでOK
        targetName = Path.GetFileName(item.Path);
    }
}
