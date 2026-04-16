using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.RightsManagement;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;

namespace SkyWingViewer.ViewModels;

public partial class DirectoryViewModel : ObservableObject
{
    public string directoryPath;

    [ObservableProperty]
    private BitmapSource? iconImage;

    public string DirectoryName { get; private set; }
    public DirectoryViewModel(String path)
    {
        directoryPath = path;
        DirectoryName = Path.GetFileName(path);
        _ = Task.Run(async () =>
        {
            await GetIconAsync(directoryPath);
        });
    }

    private async Task GetIconAsync(string path)
    {
        try
        {
            using (var shellFolder = ShellContainer.FromParsingName(path))
            {
                // アイコンを取得。サイズは Large, Medium, Small など選べる。
                IconImage = shellFolder.Thumbnail.BitmapSource;
                IconImage.Freeze();
            }
        }
        catch
        {
            //TODO:  エラー時はデフォルトアイコンなりを返す
            return;
        }

    }
}
