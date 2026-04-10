using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Models;





//WILL: 将来的に DB 等に接続対応する場合には共通の規定クラスを作りつつ、DB Target のクラスも作る
public class TargetDirectory
{
    //パス変更時の通知用
    public event Action? PathChanged;
    public string Path { get; private set; }


    //TODO: Path のバリデーションチェック
    public TargetDirectory(string path)
    {
        Path = path;
    }

    public void SetPath(string path)
    {
        Path = path;
        PathChanged?.Invoke();
    }
}


