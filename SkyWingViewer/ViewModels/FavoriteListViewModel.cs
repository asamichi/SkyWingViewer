using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkyWingViewer.ViewModels;

public partial class FavoriteListViewModel : ObservableObject,IContextMenu,IOpenCommand
{
    [ObservableProperty]
    private ObservableCollection<DirectoryModel> favoriteList;
    [ObservableProperty]
    private DirectoryModel? selectedFavorite;


    private FavoriteListService _favoriteListService;
    private TargetNavigationService _targetNavigationService;
    ILogger<FavoriteListViewModel> _logger;
    public IList<ContextMenuItem> ContextMenuItems { get; }
    public ICommand OpenCommand { get; }

    public FavoriteListViewModel(FavoriteListService favoriteListService,TargetNavigationService targetNavigationService,ILogger<FavoriteListViewModel> logger)
    {
        _favoriteListService = favoriteListService;
        _targetNavigationService = targetNavigationService;
        _logger = logger;
        FavoriteList = _favoriteListService.FavoriteList;
        ContextMenuItems = GetContextMenu();
        OpenCommand = FavoriteOpenCommand;
    }

    [RelayCommand]
    public void AddFavorite()
    {
        _favoriteListService.AddFavoriteList(_targetNavigationService.Path);
    }

    [RelayCommand]
    public void FavoriteOpen(DirectoryModel? value)
    {
        if (value == null) return;

        string targetPath = value.Path;
        if (System.IO.Directory.Exists(targetPath) == false)
        {
            MessageBox.Show("選択したフォルダが見つかりませんでした");
            _logger.LogWarning("お気に入りから選択したフォルダにアクセスできませんでした Path:{path}", targetPath);
            return;
        }

        _targetNavigationService.SetPath(targetPath);
    }

    //コンテキストメニュー用
    public IList<ContextMenuItem> GetContextMenu()
    {
        List<ContextMenuItem> list = new();
        list.Add(new ContextMenuItem("お気に入りを削除", RemoveFavoriteCommand));
        return list;
    }
    [RelayCommand]
    public void RemoveFavorite(DirectoryModel? target)
    {
        if (target == null) return;
        _favoriteListService.RemoveFavoriteList(target);
    }

}

