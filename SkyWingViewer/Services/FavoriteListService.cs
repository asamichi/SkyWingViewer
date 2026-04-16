using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SkyWingViewer.Services;
using SkyWingViewer.Commons;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace SkyWingViewer.Services;

internal class FavoriteListService
{
    //参照渡しで受け取るので大本の AppSettings 等に値を返す必要はない。また、参照の値について readonly になるという話なので、Add なんかはできる。
    readonly FavoriteListSettings _favoriteListSettings;

    public List<DirectoryModel> FavoriteList => _favoriteListSettings.FavoriteList; 

    ILogger _logger;
    public FavoriteListService(FavoriteListSettings favoriteListSettings, ILogger<FavoriteListService> logger)
    {
        _favoriteListSettings = favoriteListSettings;
        _logger = logger;
    }


    public void AddFavoriteList(string path)
    {
        if(Directory.Exists(path) == false)
        {
            MessageBox.Show("存在しないファイルパスをお気に入りリストに追加しようとしたため、操作に失敗しました。");
            _logger.LogWarning("存在しないディレクトリパスをリストに追加しようとしました。Path: {path}", path);
            return;
        }

        DirectoryModel directoryModel = new();
        directoryModel.SetPath(path);
        
        FavoriteList.Add(directoryModel);
    }


}
