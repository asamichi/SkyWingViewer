using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Windows;

namespace SkyWingViewer.Services;

public partial class TargetNavigationService : ObservableObject
{
    public string Path => _model.Path;

    private TargetDirectory _model;

    public event Action? TargetPathChanged;

    public TargetNavigationService(TargetDirectory model)
    {
        _model = model;

        //イベント登録
        _model.PathChanged += OnPathChanged;
    }


    public void SetPath(string path)
    {
        string adjustPass = AdjustPass(path);
        if (Path == adjustPass || CheckPass(adjustPass) == false)
        {
            return;
        }
        _model.SetPath(adjustPass);
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
        return path.Trim(' ', '"');
    }


    private void OnPathChanged()
    {
        //SetPath(_model.Path);
        TargetPathChanged?.Invoke();
    }


}
