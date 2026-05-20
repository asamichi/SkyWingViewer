using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace SkyWingViewer.Models;

//プロパティに Path がいるので、毎回 System.... とするのは可読性が下がるため。
using static System.IO.Path;

public abstract class FileSystemItemBase
{
    public string Path { get; set; }
    //フォルダが対象の時に、末尾が階層の区切り文字、\ とかの時の対策で、末尾をトリムする
    public string Name => GetFileName(Path.TrimEnd(DirectorySeparatorChar, AltDirectorySeparatorChar));

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
