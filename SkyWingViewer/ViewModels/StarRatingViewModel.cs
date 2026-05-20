using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using SkyWingViewer.ViewModels.Interfaces;
using SkyWingViewer.Models;

namespace SkyWingViewer.ViewModels;

public partial class StarRatingViewModel: StarRatingBase
{

    private ILogger _logger;

    private AssetSelectionService _assetSelectionService;
    private StarRatingService _starRatingService;

    public StarRatingViewModel(ILogger<StarRatingViewModel> logger,AssetSelectionService assetSelectionService, StarRatingService starRatingService) : base()
    {
        _assetSelectionService = assetSelectionService;
        _starRatingService = starRatingService;
        _logger = logger;
        _assetSelectionService.TargetItemsChanged += OnTargetItemsChanged;

    }

    private async Task SetRateAsync(int rate)
    {
        if(rate < 0 || 5 < rate)
        {
            _logger.LogWarning("不正なレートが設定されようとしています。 rate = {rate}", rate);
        }

        if(Rate == rate)
        {
            rate = 0;
        }

        //DB に反映
        await _starRatingService.BulkSetRateAsync(_assetSelectionService.TargetItems, rate);
        
        //WILL: DB 登録済みのアセットは DB からも読むように変更したら実装
        //今あるリストが保持する FileSystemItemBase に反映
        //foreach(FileSystemItemBase item  in _assetSelectionService.TargetItems)
        //{
            
        //}

        Rate = rate;
        UpdateStars();
    }

    [RelayCommand]
    private async Task ChangeRate(int rate)
    {
        await SetRateAsync(rate);
    }

    public void OnTargetItemsChanged()
    {
        _ = GetRateFromAssetsAsync();
    }

    public async Task GetRateFromAssetsAsync()
    {
        try
        {
            Rate = await _starRatingService.GetRateFromAssetsAsync(_assetSelectionService.TargetItems);
            UpdateStars();
        }
        catch (Exception ex)
        {
            _logger.LogError("GetRateFromAssetsAsync の中で例外が発生しました。{ex}", ex);
        }
    }
}
