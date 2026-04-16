using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace SkyWingViewer.Services;

public interface IThumbnailProvider
{
    HashSet<string> SupportedExtensions { get; }
    public BitmapImage GetBitmapImage(string filePath);
}
