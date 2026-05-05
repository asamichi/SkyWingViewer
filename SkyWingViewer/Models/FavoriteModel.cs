using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SkyWingViewer.Models;

public class FavoriteModel
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Path { get; set; }

    public FavoriteModel(string path)
    {
        Path = path;
    }
}
