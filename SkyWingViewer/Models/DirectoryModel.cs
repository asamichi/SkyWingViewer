using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SkyWingViewer.Models;

public class DirectoryModel
{
    public string Path { get; set; }
    public string Name => System.IO.Path.GetFileName(Path);

    public ItemMetadata Metadata { get; set; }

    public DirectoryModel(string path)
    {
        Path = path;
        FileSystemInfo fileSystemInfo = new DirectoryInfo(path);
        Metadata = new ItemMetadata(fileSystemInfo);
    }

}
