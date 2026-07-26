using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.IdentityModel.Tokens;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace SkyWingViewer.ViewModels;

public partial class TargetPathBarViewModel : ObservableObject
{
    private TargetNavigationService _targetNavigationService;

    [ObservableProperty]
    public string? targetPath;


    public class Breadcrumb
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Breadcrumb(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
    //パンくずリスト
    public ObservableCollection<Breadcrumb> BreadcrumbList { get; } = new();

    public TargetPathBarViewModel(TargetNavigationService targetNavigationService)
    {
        _targetNavigationService = targetNavigationService;
        targetPath = _targetNavigationService.Path;

        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
        CreateBreadcrumb(TargetPath);

    }

    //こちらは ナビゲーションサービスの TargetPathChanged に登録されるほう
    private void OnTargetPathChanged()
    {
        TargetPath = _targetNavigationService.Path;
        CreateBreadcrumb(TargetPath);
    }

    //こちらは This.TargetPath が変更されたときに発火
    partial void OnTargetPathChanged(string? value)
    {
        //本来は SetPath でチェック入るのでいらないけどエディタの警告消し
        if (value == null) return;

        _targetNavigationService.SetPath(value);

    }


    // ここからパンくずリスト実装


    //リストのボタン押下時
    [RelayCommand]
    private void TargetPathChange(string path)
    {
        OnTargetPathChanged(path);
    }

    private void CreateBreadcrumb(string path)
    {
        BreadcrumbList.Clear();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        Stack<Breadcrumb> stack = new();
        

        DirectoryInfo? current = directoryInfo;

        //一旦末尾から stack に登録していって、最後に表示用のコレクションに stack からポップしていく

        //親が無くなるまで親に進む
        while(current != null)
        {
            string name = current.Name;
            name = name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            stack.Push(new Breadcrumb(name, current.FullName));
            //BreadcrumbList.Add(new Breadcrumb(name, current.FullName));
            current = current.Parent;
        }

        while (stack.Count > 0)
        {
            BreadcrumbList.Add(stack.Pop());
        }

    }


}
