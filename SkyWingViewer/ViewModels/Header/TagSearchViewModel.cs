using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace SkyWingViewer.ViewModels;


public partial class TagSearchViewModel : ObservableObject,IPopupControl
{
    [ObservableProperty]
    private ObservableCollection<TagCheckBox> tagList = new();

    [ObservableProperty]
    public string searchWord = "";

    private ItemTagService _itemTagService;
    private ItemSearchService _itemSearchService;
    private ILogger _logger;

    public TagSearchViewModel(ItemTagService itemTagService,ItemSearchService itemSearchService,ILogger<TagSearchViewModel> logger)
    {
        _itemTagService = itemTagService;
        _itemSearchService = itemSearchService;
        _logger = logger;

        _ = CreateTagListAsync();
    }

    //タグリスト生成
    private async Task CreateTagListAsync()
    {
        try
        {
            List<ItemTagModel> list = await _itemTagService.GetAllTagsAsync();
            List<TagCheckBox> checkBoxList = list.Select(t => new TagCheckBox(t,_itemSearchService.searchTagNames.Contains(t.TagName),OnCeckBoxChanged)).ToList();
            TagList = new ObservableCollection<TagCheckBox>(checkBoxList);
        }
        catch(Exception ex)
        {
            _logger.LogError("CreateTagListAsync の際に例外が発生しました。{ex}", ex);

        }
    }

    private void OnCeckBoxChanged()
    {
        _itemSearchService.SearchTags = TagList.Where(t => t.IsChecked == true).Select(t => t.TagModel).ToList();
    }

}

public partial class TagCheckBox : ObservableObject
{
    public ItemTagModel TagModel { get; set; }

    public string TagName => TagModel.TagName;

    [ObservableProperty]
    private bool isChecked;

    private Action _onIsCheckedChanged;

    public TagCheckBox(ItemTagModel tagModel, bool isChecked, Action onIsCheckedChanged)
    {
        _onIsCheckedChanged = onIsCheckedChanged;
        TagModel = tagModel;
        //イベントを発火させないためパッキングフィールドに代入
        this.isChecked = isChecked;
    }
    partial void OnIsCheckedChanged(bool value)
    {
        _onIsCheckedChanged.Invoke();
    }

}