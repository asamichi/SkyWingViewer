using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SkyWingViewer.Services;
using SkyWingViewer.Views.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class FilterButtonAreaViewModel : ObservableObject
{

    private PopupService _popupService;
    private IServiceProvider _serviceProvider;

    public StarRatingFilterViewModel StarRatingFilterViewModel { get; set; }

    private ItemSearchService _itemSearchService;

    [ObservableProperty]
    public int searchTagNum = 0;

    public FilterButtonAreaViewModel(PopupService popupService, IServiceProvider serviceProvider,StarRatingFilterViewModel starRatingFilterViewModel,ItemSearchService itemSearchService)
    {
        StarRatingFilterViewModel = starRatingFilterViewModel;
        _popupService = popupService;
        _serviceProvider = serviceProvider;
        _itemSearchService = itemSearchService;

        _itemSearchService.SearchTagsChanged += OnSearchTagsChanged;
    }

    [RelayCommand]
    public void FilterButton(object parameter)
    {
        _popupService.CreatePopup(_serviceProvider.GetRequiredService<TagSearchViewModel>(), parameter);
    }

    public void OnSearchTagsChanged()
    {
        SearchTagNum = _itemSearchService.SearchTags.Count;
    }
}
