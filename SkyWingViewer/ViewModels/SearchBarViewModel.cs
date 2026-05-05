using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using SkyWingViewer.Services;
using System.Diagnostics;

namespace SkyWingViewer.ViewModels;


//TODO: 検索ワードをクリアできる X ボタンの配置等
public partial class SearchBarViewModel : ObservableObject
{

    [ObservableProperty]
    private string? searchValue;

    ItemSearchService _itemSearchService;

    TargetNavigationService _targetNavigationService;


    public SearchBarViewModel(ItemSearchService itemSearchService, TargetNavigationService tarNavigationService)
    {
        searchValue = "";
        _itemSearchService = itemSearchService;
        _targetNavigationService = tarNavigationService;

        //イベント登録
        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
    }

    partial void OnSearchValueChanged(string? value)
    {
        if (SearchValue == null)
        {
            _itemSearchService.SearchWords.Clear();
            return;
        }

        //区切り文字はスペース（半角 / 全角）
        _itemSearchService.SearchWords = SearchValue.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public void OnTargetPathChanged()
    {
        SearchValue = "";
    }
}
