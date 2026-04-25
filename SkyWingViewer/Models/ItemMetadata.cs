using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SkyWingViewer.Models;

//基本的な情報。検索時の対象となり得る想定
public class ItemMetadata
{
    public string Name { get; set; }
    public DateTime CreatonTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastModifiedTime{ get; set; }

    public long? Length { get; set; }

    //以下は未実装
    public int rate { get; set; }
    public List<String> Tags;

    public ItemMetadata(FileSystemInfo fileSystemInfo)
    {
        Name = fileSystemInfo.Name; 
        CreatonTime = fileSystemInfo.CreationTime;
        LastAccessTime = fileSystemInfo.LastAccessTime;
        LastModifiedTime = fileSystemInfo.LastWriteTime;

        if(fileSystemInfo is FileInfo fileInfo)
        {
            Length = fileInfo.Length;
        }
        else
        {
            Length = null;
        }
    }

    //public ItemMetadata(string name, DateTime creationTime, DateTime lastAccessTime, DateTime lastModifiedTime, long? length = null)
    //{
    //    Name = name;
    //    CreatonTime = creationTime;
    //    LastAccessTime = lastAccessTime;
    //    LastModifiedTime = lastModifiedTime;
    //    Length = length;
    //}
}
