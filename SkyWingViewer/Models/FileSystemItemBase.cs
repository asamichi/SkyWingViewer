using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace SkyWingViewer.Models;

public abstract class FileSystemItemBase
{
    public string Path { get; set; }
    public string Name => System.IO.Path.GetFileName(Path);

    public ItemMetadata Metadata { get; set; }

    public FileSystemItemBase(string path)
    {
        Path = path;

        FileAttributes fileAttributes = File.GetAttributes(Path);

        //フォルダかどうか判定するビットだけ抜き出して比較する必要がある
        if((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
        {
            //ディレクトリ
            FileSystemInfo fileSystemInfo = new DirectoryInfo(Path);
            Metadata = new ItemMetadata(fileSystemInfo);
        }
        else
        {
            //ファイル
            FileInfo fileInfo = new FileInfo(Path);
            Metadata = new ItemMetadata(fileInfo);
        }
    }

}
