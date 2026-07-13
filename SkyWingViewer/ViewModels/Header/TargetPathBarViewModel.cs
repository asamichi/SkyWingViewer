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
    private TargetNavigationService _targetNavigationService;

    [ObservableProperty]
    public string? targetPath;

    public TargetPathBarViewModel(TargetNavigationService targetNavigationService)
    {
        _targetNavigationService = targetNavigationService;
        targetPath = _targetNavigationService.Path;

        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;

    }

    //こちらは _model.TargetPathChanged に登録されるほう
    private void OnTargetPathChanged()
    {
        TargetPath = _targetNavigationService.Path;
    }

    //こちらは This.TargetPath が変更されたときに発火
    partial void OnTargetPathChanged(string? value)
    {
        //本来は SetPath でチェック入るのでいらないけどエディタの警告消し
        if (value == null) return;

        _targetNavigationService.SetPath(value);

    }
}
