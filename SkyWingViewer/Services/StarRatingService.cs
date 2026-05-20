using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SkyWingViewer.Services;

public class StarRatingService
{
    DatabaseService _databaseService;
    private ILogger _logger;
    TargetNavigationService _targetNavigationService;
    
    public StarRatingService(DatabaseService databaseService,ILogger<StarRatingService> logger,TargetNavigationService targetNavigationService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _targetNavigationService = targetNavigationService;
    }

    /* *************** 登録 ************** */
    public async Task BulkSetRateAsync(IEnumerable<FileSystemItemBase> items, int rate)
    {
        await _databaseService.BulkSetRateAsync(items, rate);
        foreach (FileSystemItemBase item in items)
        {
            item.Metadata.Rating = rate;
        }
    }


    /* *************** 読み取り ************** */

    public async Task<int> GetRateFromAssetsAsync(IEnumerable<FileSystemItemBase> assets)
    {
        try
        {
            return await _databaseService.GetRateFromAssetsAsync(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetRateFromAssetsAsync の際に例外が発生しました。{ex}", ex);
            return 0;
        }
    }

    public async Task SetRateFromParentPathAsync(IEnumerable<FileSystemItemBase> assets)
    {
        Dictionary<string, int> Rates = await _databaseService.GetRateFromParentPathAsync(assets,_targetNavigationService.Path);

        foreach(FileSystemItemBase item in assets)
        {
            if(Rates.TryGetValue(item.Path, out int rate))
            {
                item.Metadata.Rating = rate;
            }
            //Metadata.Rating はデフォルトで 0 なので、存在しないなら 0 のままでよい
        }
    }

}
