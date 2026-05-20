using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Messeages;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class TagCheckBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string tagName;

    [ObservableProperty]
    private bool isChecked = false;

    private ItemTagService _itemTagService;

    private AssetSelectionService _assetSelectionService;

    private readonly IMessenger _messenger;
    private ILogger _logger;

    public TagCheckBoxViewModel(string tagName,bool isChecked, ItemTagService itemTagService, AssetSelectionService assetSelectionService,IMessenger messenger,ILogger<TagCheckBoxViewModel> logger)
    {
        _logger = logger;
        _itemTagService = itemTagService;
        _assetSelectionService = assetSelectionService;
        _messenger = messenger;
        TagName = tagName;
        IsChecked = isChecked;
    }


    partial void OnIsCheckedChanged(bool value)
    {
        if (value == true)
        {
            _ = BulkAttachTagToAssetAsync();
        }
        else
        {
            _ = BulkDetachTagToAssetAsync();
        }
        _messenger.Send(new EditTagOnPopup(TagName, value));

    }


    private async Task BulkAttachTagToAssetAsync()
    {
        try
        {
            await _itemTagService.BulkAttachTagToAssetAsync(_assetSelectionService.TargetItems, TagName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("BulkAttachTagToAssetAsync の中で例外が発生しました。{ex}", ex);
        }
    }

    private async Task BulkDetachTagToAssetAsync()
    {
        try
        {
            await _itemTagService.BulkDetachTagToAssetAsync(_assetSelectionService.TargetItems, TagName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("BulkDetachTagToAssetAsync の中で例外が発生しました。{ex}", ex);
        }
    }
}
