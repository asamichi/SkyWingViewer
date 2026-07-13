using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.Services;

public  class LibraryService
{
    public ObservableCollection<LibraryModel> LibraryList = new();

    private DatabaseService _databaseService;
    ILogger _logger;
    public LibraryService(ILogger<LibraryService> logger,DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;

        //foreach (LibraryModel lib in _databaseService.GetLibraryList())
        //{
        //    LibraryList.Add(lib);
        //}
        _databaseService.InitComplete += OnInitComplete;
    }


    public void OnInitComplete()
    {
        LibraryList.Clear();
        foreach (LibraryModel lib in _databaseService.GetLibraryList())
        {
            LibraryList.Add(lib);
        }
    }

    public async Task AddLibraryListAsync(string name,string path)
    {
        LibraryModel? lib = await _databaseService.CreateLibraryAsync(name,path);
        if(lib == null)
        {
            return;
        }
        LibraryList.Add(lib);
    }

    public async Task RemoveLibraryListAsync(LibraryModel target)
    {
        LibraryModel? targetLib = LibraryList.FirstOrDefault(l => l.Name == target.Name && l.RootPath == target.RootPath);
        if (targetLib == null)
        {
            _logger.LogWarning("RemoveLibraryListAsync にて、存在しないライブラリの削除命令が実施されました。");
            return;
        }
        await _databaseService.UnregisterLibraryAsync(target);
        LibraryList.Remove(targetLib);
    }

    public async Task MoveLibraryAsync(LibraryModel target, string libPath)
    {
        Result<LibraryModel> result = await _databaseService.UpdateLibraryAsync(target.Name, libPath);
        if(result.ResultCode == ResultCode.Success)
        {
            target.RootPath = libPath;
        }
    }


}
