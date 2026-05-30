using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace SkyWingViewer.Services;

public class MemoService
{
    private DatabaseService _databaseService;
    private ILogger _logger;

    public MemoService(DatabaseService databaseService, ILogger<MemoService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /* *************** 書き込み ************** */

    public async Task SetMemoAsync(IEnumerable<FileSystemItemBase> items, string memo)
    {
        await _databaseService.SetMemoAsync(items, memo);
    }


    /* *************** 読み取り ************** */
    public async Task<string?> GetMemoAsync(FileSystemItemBase asset)
    {
        return await _databaseService.GetMemoAsync(asset);
    }


}
