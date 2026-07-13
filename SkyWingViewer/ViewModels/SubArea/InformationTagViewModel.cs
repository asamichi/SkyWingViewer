using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Messeages;
using SkyWingViewer.Services;
using SkyWingViewer.Views.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class InformationTagViewModel : ObservableObject,IRecipient<EditTagOnPopup>
{

    private PopupService _popupService;
    private IServiceProvider _serviceProvider;

    private ItemTagService _itemTagService;
    private AssetSelectionService _assetSelectionService;

    [ObservableProperty]
    public ObservableCollection<string> tags = new();

    [ObservableProperty]
    public int addTagBtnHideFlag= 1;

    private readonly IMessenger _messenger;
    private ILogger _logger;

    public InformationTagViewModel(PopupService popupService,IServiceProvider serviceProvider,ItemTagService itemTagService,AssetSelectionService assetSelectionService,IMessenger messenger,ILogger<InformationTagViewModel> logger)
    {
        _logger = logger;
        _popupService = popupService;
        _serviceProvider = serviceProvider;
        _itemTagService = itemTagService;
        _assetSelectionService = assetSelectionService;
        _messenger = messenger;
        _messenger.RegisterAll(this);

        _assetSelectionService.TargetItemsChanged += OnTargetItemsChanged;

        _ = SetTags();

    }

    private async Task SetTags()
    {
        try
        {
            HashSet<string> tagList = await _itemTagService.GetIntersectTagsFromAssetListAsync(_assetSelectionService.TargetItems);
            Tags = new ObservableCollection<string>(tagList);
        }
        catch (Exception ex)
        {
            _logger.LogError("SetTags の中で例外が発生しました。{ex}", ex);
        }

    }

    [RelayCommand]
    public void OpenTagEdit(object parameter)
    {
        _popupService.CreatePopup(_serviceProvider.GetRequiredService<TagEditViewModel>(),parameter);

    }

    public void OnTargetItemsChanged()
    {
        _ = SetTags();
        if(_assetSelectionService.TargetItems.Count > 0)
        {
            AddTagBtnHideFlag = 0;
        }
        else
        {
            AddTagBtnHideFlag = 1;
        }
    }

    [RelayCommand]
    public async Task DetachTag(object parameter)
    {
        string? tagName = parameter as string;
        if (tagName == null) return;

        await _itemTagService.BulkDetachTagToAssetAsync(_assetSelectionService.TargetItems, tagName);
        Tags.Remove(tagName);

    }

    public void Receive(EditTagOnPopup msg)
    {
        if(msg.isAdd == true)
        {
            if(Tags.Contains(msg.tagName) == false)
            {
                Tags.Add(msg.tagName);
            }
        }
        else
        {
            if(Tags.Contains(msg.tagName) == true)
            {
                Tags.Remove(msg.tagName);
            }
        }
    }
}

