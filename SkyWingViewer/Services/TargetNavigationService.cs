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
        _model.SetPath(path);
    }

    private void OnPathChanged()
    {
        //SetPath(_model.Path);
        TargetPathChanged?.Invoke();
    }


}
