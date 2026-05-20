using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class SideMenuAreaViewModel : ObservableObject
{

    public FavoriteListViewModel FavoriteListViewModel { get; set; }
    private IPopupService _popupService;
    private IServiceProvider _serviceProvider;


    public SideMenuAreaViewModel(FavoriteListViewModel favoriteListViewModel,IPopupService popupService,IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _popupService = popupService;
        FavoriteListViewModel = favoriteListViewModel;
    }


    [RelayCommand]
    public void OpenTagList(object parameter)
    {
        _popupService.CreatePopup(_serviceProvider.GetRequiredService<TagListViewModel>(), parameter, new PopupServiceOptions()
        {
            PlacementMode = System.Windows.Controls.Primitives.PlacementMode.Right
        });
    }
}
