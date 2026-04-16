using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SkyWingViewer.Models;

public class DirectoryModel
{
    public string Path { get; set; }
    public string Name => System.IO.Path.GetDirectoryName(Path);

    public void SetPath(string path)
    {
        if (System.IO.Directory.Exists(path) == false)
        {
            throw new System.IO.DirectoryNotFoundException($"パスが見つかりません: {path}");
        }
        Path = path;
    }
}
