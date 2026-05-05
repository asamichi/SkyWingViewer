using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace SkyWingViewer.ViewModels;

public partial class SortAreaViewModel : ObservableObject
{
    //選択されているオプション
    [ObservableProperty]
    private string selectedOption;

    //表示するリスト
    public ObservableCollection<string> SortOptions { get; private set; } = new();


    public ItemSortService _itemSortService;
    
    public SortAreaViewModel(ItemSortService itemSortService)
    {
        _itemSortService = itemSortService;

        foreach(SortType sortType in _itemSortService.SortTypes)
        {
            SortOptions.Add(sortType.SortName);
        }

        selectedOption = SortOptions[0];
    }

    partial void OnSelectedOptionChanged(string value)
    {
        _itemSortService.SortKey = _itemSortService.SortTypes.FirstOrDefault(x => x.SortName == value) ?? _itemSortService.SortTypes.First();
    }

}
