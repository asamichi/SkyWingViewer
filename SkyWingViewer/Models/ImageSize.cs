using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Models;

public record struct ImageSize
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ImageSize(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
