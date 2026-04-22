using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Shell;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.RightsManagement;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;

namespace SkyWingViewer.ViewModels;

public partial class DirectoryViewModel : ObservableObject, IOpenCommand, IContextMenu
{
    public string directoryPath => _model.Path;
    private DirectoryModel _model;

    [ObservableProperty]
    private BitmapSource? iconImage;

    private ILogger<DirectoryViewModel> _logger;

    public ICommand OpenCommand { get; }
    public IList<ContextMenuItem> ContextMenuItems { get; }

    private TargetNavigationService _targetNavigationService;

    public string DirectoryName { get; private set; }

    public DirectoryViewModel(DirectoryModel directory,ILogger<DirectoryViewModel> logger, TargetNavigationService targetNavigationService)
    {
        _logger = logger;
        _model = directory;
        _targetNavigationService = targetNavigationService;

        DirectoryName = Path.GetFileName(directoryPath);
        _ = Task.Run(async () =>
        {
            await GetIconAsync(directoryPath);
        });

        OpenCommand = DirectoryOpenCommand;
        ContextMenuItems = GetDefaultContextMenu();

    }

    private async Task GetIconAsync(string path)
    {
        _logger.LogTrace("アイコンの取得を開始します。Path: {path}", path);

        //TODO: アイコンの取得もちゃんと並列化したい。一旦仮で不便ない程度の実装
        IconImage = await Task<BitmapSource?>.Run(() =>
        {
            try
            {
                using (var shellFolder = ShellContainer.FromParsingName(path))
                {
                    // アイコンを取得。サイズは Large, Medium, Small など選べる。
                    BitmapSource bitmap = shellFolder.Thumbnail.BitmapSource;
                    bitmap.Freeze();
                    _logger.LogTrace("アイコンの取得が完了しました。Path : {path}", path);
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("LoadThumbnail にてエラーが発生しました : {ex}", ex);
                //TODO:  エラー時はデフォルトアイコンなりを返す
                return null;
            }
        }
        );
    }

    [RelayCommand]
    public void DirectoryOpen()
    {
        _targetNavigationService.SetPath(directoryPath);
    }

    //コンテキストメニュー用
    //デフォルトの右クリックメニューを取得。子クラスで色々順番いじるときに便利かもなのでメソッドに切り分けておく
    public IList<ContextMenuItem> GetDefaultContextMenu()
    {
        List<ContextMenuItem> list = new();

        list.Add(new ContextMenuItem("エクスプローラーで開く", OpenExplorerCommand));
        list.Add(new ContextMenuItem("パスをコピー", CopyPathCommand));
        return list;
    }

    [RelayCommand]
    public void OpenExplorer()
    {
        Process.Start("explorer.exe", $"\"{directoryPath}\"");
    }

    [RelayCommand]
    public void CopyPath()
    {
        Clipboard.SetText(directoryPath);
    }
}
