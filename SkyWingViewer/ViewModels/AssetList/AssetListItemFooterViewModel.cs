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
        //末尾 \ には対応しないが、ユーザーがパスを指定することは無いのでこれで大丈夫
        TargetName = item.Name;
    }
}
