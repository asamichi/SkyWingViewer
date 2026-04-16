using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace SkyWingViewer.ViewModels;

public partial class TargetPathBarViewModel : ObservableObject
{
    private TargetNavigationService _model;

    [ObservableProperty]
    public string? targetPath;

    public TargetPathBarViewModel(TargetNavigationService model)
    {
        _model = model;
        targetPath = _model.Path;

        //イベント登録
        _model.TargetPathChanged += OnTargetPathChanged;

    }

    //こちらは _model.TargetPathChanged に登録されるほう
    private void OnTargetPathChanged()
    {
        TargetPath = _model.Path;
    }

    //こちらは This.TargetPath が変更されたときに発火
    partial void OnTargetPathChanged(string? value)
    {
        //本来は SetPath でチェック入るのでいらないけどエディタの警告消し
        if (value == null) return;

        _model.SetPath(value);
    }
}
