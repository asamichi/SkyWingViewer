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
public abstract partial class AssetViewModelBase : ObservableObject, IOpenCommand, IContextMenu, IItemInformationProvider
{
    protected readonly AssetBase _asset;
    public string AssetPath => _asset.AssetPath;

    protected readonly AssetListItemFooterViewModel _footer;
    public virtual ICommand OpenCommand { get; } 
    public virtual IList<ContextMenuItem> ContextMenuItems { get; }
    public AssetListItemFooterViewModel Footer => _footer;

    public List<ItemInformation> InformationItems { get; private set; }
    protected AssetViewModelBase(AssetBase asset)
    {
        _asset = asset;
        _footer = new AssetListItemFooterViewModel(asset);

        //デフォルト動作として、OS の関連付けに従ってアセットを開くようにしておく
        OpenCommand = new RelayCommand(() =>
            Process.Start(new ProcessStartInfo(_asset.AssetPath) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_asset.AssetPath) })
            );

        ContextMenuItems = GetDefaultContextMenu();
    }

    public void CreateInformationItems()
    {
        InformationItems = ItemInformationService.ConvertItemMetadataToItemInformations(_asset.Metadata);
    }



    //コンテキストメニュー用
    //デフォルトの右クリックメニューを取得。子クラスで色々順番いじるときに便利かもなのでメソッドに切り分けておく
    public IList<ContextMenuItem> GetDefaultContextMenu()
    {
        List<ContextMenuItem> list = new();

        list.Add(new ContextMenuItem("エクスプローラーで表示", OpenExplorerCommand));
        list.Add(new ContextMenuItem("パスをコピー", CopyPathCommand));
        return list;
    }

    [RelayCommand]
    public void OpenExplorer()
    {
        Process.Start("explorer.exe", $"/select,\"{_asset.AssetPath}\"");
    }

    [RelayCommand]
    public void CopyPath()
    {
        Clipboard.SetText(_asset.AssetPath);
    }
}
