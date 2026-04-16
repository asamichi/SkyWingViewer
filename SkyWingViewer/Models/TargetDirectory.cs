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


    //TODO: Path のバリデーションチェック
    public TargetDirectory(string path)
    {
        Path = System.IO.Path.GetFullPath(AdjustPass(path));
    }

    public void SetPath(string path)
    {
        string adjustPass = AdjustPass(path);
        if (Path == adjustPass) return;
        if (CheckPass(adjustPass) == false) return;

        Path = System.IO.Path.GetFullPath(adjustPass);
        PathChanged?.Invoke();
    }

    public bool CheckPass(string path)
    {
        //変更の必要が無いなら return
        if (path == Path) return false;

        //存在しないなら false
        if (Directory.Exists(path) == false)
        {
            MessageBox.Show("存在しないパスが入力されました。");
            return false;
        }
        try
        {
            //バリデーションとして使用。エラー吐いたら false。末尾空白などへの対処（Exists は末尾空白を通す）。
            System.IO.Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(@$"無効なパスです : {ex}");
            return false;
        }
        return true;
    }

    public string AdjustPass(string path)
    {
        //Windows 標準機能のパスをコピーでコピーした時、両脇につくのでアプリ側で対応した方が体験が良いと判断。その他ゴミで入りそうなのを trim。
        return path.Trim(' ','"');
    }

}


