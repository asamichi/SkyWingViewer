using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using SkyWingViewer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SkyWingViewer.ViewModels;

public partial class TagListViewModel : ObservableObject, IPopupControl
{
    //登録済みタグのリスト
    [ObservableProperty]
    private ObservableCollection<TagLabelViewModel> tagList = new();

    private ItemTagService _itemTagService;
    private ILogger _logger;
    private IServiceProvider _serviceProvider;

    public TagListViewModel(ItemTagService itemTagService,ILogger<TagListViewModel> logger, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _itemTagService = itemTagService;
        

        _ = SetAllTagsAsync();

    }


    public async Task SetAllTagsAsync()
    {
        try
        {
            List<ItemTagModel> list = await _itemTagService.GetAllTagsAsync();
            foreach (ItemTagModel itemTag in list)
            {
                TagList.Add(ActivatorUtilities.CreateInstance<TagLabelViewModel>(_serviceProvider,itemTag));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("SetAllTagsAsync の際に例外が発生しました。{ex}", ex);

        }
    }



}
