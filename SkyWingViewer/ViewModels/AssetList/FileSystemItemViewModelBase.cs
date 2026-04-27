using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Diagnostics;
using System.IO;


namespace SkyWingViewer.ViewModels;

//この血統の返り値を持つメソッドの型のために必要。
public partial class FileSystemItemViewModelBase : ObservableObject
{

}
public partial class FileSystemItemViewModelBase<TModel> : FileSystemItemViewModelBase, IOpenCommand, IContextMenu, IItemInformationProvider where TModel : FileSystemItemBase
{
    //対応するモデル
    protected TModel _model { get; private set; }
    public string ItemPath => _model.Path;

    //サムネイル
    [ObservableProperty]
    private BitmapSource? thumbnail;

    //開く
    public virtual ICommand OpenCommand { get; set; }
    //右クリックメニュー
    public virtual IList<ContextMenuItem> ContextMenuItems { get; }
    //詳細情報のラベルと値のリスト
    public List<ItemInformation>? InformationItems { get; private set; }

    //フッター
    public AssetListItemFooterViewModel Footer { get; private set;}

    public FileSystemItemViewModelBase(TModel model)
    {
        _model = model;
        Footer = new AssetListItemFooterViewModel(_model);

        //デフォルト動作として、OS の関連付けに従ってアセットを開くようにしておく
        OpenCommand = new RelayCommand(() =>
            Process.Start(new ProcessStartInfo(_model.Path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(_model.Path) })
            );

        //右クリックメニュー登録
        ContextMenuItems = GetDefaultContextMenu();
    }

    //詳細情報の生成
    public void CreateInformationItems()
    {
        InformationItems = ItemInformationService.ConvertItemMetadataToItemInformations(_model.Metadata);
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
        Process.Start("explorer.exe", $"/select,\"{_model.Path}\"");
    }

    [RelayCommand]
    public void CopyPath()
    {
        Clipboard.SetText(_model.Path);
    }

}
