using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace SkyWingViewer.Services.ReadImageFileToBitmapImage;

public class CreateBitmapFromShellFileToDirectory
{
    public static async Task<BitmapSource?> GetBitmapImage(string filePath)
    {
        using (var shellFolder = ShellContainer.FromParsingName(filePath))
        {
            // アイコンを取得。サイズは Large, Medium, Small など選べる。
            BitmapSource bitmap = shellFolder.Thumbnail.BitmapSource;
            bitmap.Freeze();
            return bitmap;
        }
    }

}
