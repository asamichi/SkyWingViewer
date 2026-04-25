using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace SkyWingViewer.Models;

//WILL: 将来的に DB 等に接続対応する場合には共通の規定クラスを作りつつ、DB Target のクラスも作る
public class TargetDirectory
{
    //パス変更時の通知用
    public event Action? PathChanged;
    public string Path { get; private set; }


    public TargetDirectory(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
    }

    public void SetPath(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        PathChanged?.Invoke();
    }


}


