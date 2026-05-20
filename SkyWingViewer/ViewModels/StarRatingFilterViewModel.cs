using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Services;
using SkyWingViewer.ViewModels.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial  class StarRatingFilterViewModel : StarRatingBase
{
    private ItemSearchService _itemSearchService;
    private TargetNavigationService _targetNavigationService;

    private ILogger _logger;

    public StarRatingFilterViewModel(ItemSearchService itemSearchService,ILogger<StarRatingFilterViewModel> logger, TargetNavigationService targetNavigationService) : base()
    {
        _itemSearchService = itemSearchService;
        _logger = logger;
        _targetNavigationService = targetNavigationService;

        _targetNavigationService.TargetPathChanged += OnTargetPathChanged;
    }

    [RelayCommand]
    private void ChangeRate(int rate)
    {
        if (rate < 0 || 5 < rate)
        {
            _logger.LogWarning("不正なレートが設定されようとしています。 rate = {rate}", rate);
        }

        if (Rate == rate)
        {
            rate = 0;
        }

        _itemSearchService.SearchStarRate = rate;
        Rate = rate;
        UpdateStars();
    }

    public void OnTargetPathChanged()
    {
        Rate = 0;
        UpdateStars();
    }


}
