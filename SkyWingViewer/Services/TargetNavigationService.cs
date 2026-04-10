using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;

public partial class TargetNavigationService : ObservableObject
{
    public string Path { get; private set; }

    private TargetDirectory _model;

    public event Action? TargetPathChanged;

    public TargetNavigationService(TargetDirectory model)
    {
        _model = model;
        Path = _model.Path;

        //イベント登録
        _model.PathChanged += OnPathChanged;
    }


    //TODO: ディレクトリの存在判定。
    public void SetPath(string path)
    {
        Path = path;
        TargetPathChanged?.Invoke();
    }

    private void OnPathChanged()
    {
        SetPath(_model.Path);
    }


}
