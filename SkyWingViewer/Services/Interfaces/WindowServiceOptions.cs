using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SkyWingViewer.Services;

public class WindowServiceOptions
{
    public string Title { get; set; } = "SkyWingViewer - サブウィンドウ";
    public SizeToContent SizeToContent { get; set; } = SizeToContent.WidthAndHeight;
    public double? Width { get; set; } = null;
    public double? Height { get; set; } = null;

    public bool IsMaximized { get; set; } = false;
}
