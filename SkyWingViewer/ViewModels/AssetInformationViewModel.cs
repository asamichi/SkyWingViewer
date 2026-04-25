using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using SkyWingViewer.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SkyWingViewer.ViewModels;

public partial class AssetInformationViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ItemInformation> informationItem;

    private ItemInformationService _itemInformationService;

    public AssetInformationViewModel(ItemInformationService itemInformationService)
    {
        _itemInformationService = itemInformationService;
        _itemInformationService.InformationItemChanged += OnInformationItemChanged;
    }


    public void OnInformationItemChanged()
    {
        InformationItem = _itemInformationService.InformationItem;
    }

}
