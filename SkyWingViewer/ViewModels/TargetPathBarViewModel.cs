using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

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

    private void OnTargetPathChanged()
    {
        TargetPath = _model.Path;
    }

    //TODO: newValue のバリデーションチェック
    partial void OnTargetPathChanged(string? value)
    {
        if (value == null) return;
        _model.SetPath(value);
    }
}
