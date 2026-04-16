using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SkyWingViewer.Services;

public class CreateBitmapFromShellFile
{

    public static BitmapSource GetBitmapImage(string filePath)
    {

        using (var shellFile = ShellFile.FromFilePath(filePath))
        {
            var bitmap = shellFile.Thumbnail.BitmapSource;
            bitmap.Freeze();
            return (BitmapSource)bitmap;
        }

    }

}
