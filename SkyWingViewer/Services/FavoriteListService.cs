using Microsoft.Extensions.Logging;
using SkyWingViewer.Commons;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace SkyWingViewer.Services;

public class FavoriteListService
{
    //参照渡しで受け取るので大本の AppSettings 等に値を返す必要はない。また、参照の値について readonly になるという話なので、Add なんかはできる。
    readonly FavoriteListSettings _favoriteListSettings;
    readonly AppSettings _appSettings;
    readonly JsonStorage<AppSettings> _storage;

    public ObservableCollection<DirectoryModel> FavoriteList => _favoriteListSettings.FavoriteList;



    ILogger _logger;
    public FavoriteListService(AppSettings appSettings, ILogger<FavoriteListService> logger,JsonStorage<AppSettings> storage)
    {
        _appSettings = appSettings;
        _favoriteListSettings = _appSettings.FavoriteList;
        _logger = logger;
        _storage = storage;
    }


    public void AddFavoriteList(string path)
    {
        if(Directory.Exists(path) == false)
        {
            MessageBox.Show("存在しないファイルパスをお気に入りリストに追加しようとしたため、操作に失敗しました。");
            _logger.LogWarning("存在しないディレクトリパスをリストに追加しようとしました。Path: {path}", path);
            return;
        }

        _logger.LogInformation("お気に入りに追加します。Path: {path}", path);
        DirectoryModel directoryModel = new(path);
        //directoryModel.SetPath(path);
        
        FavoriteList.Add(directoryModel);
        _storage.SaveJson(_appSettings);
    }


    public void RemoveFavoriteList(DirectoryModel target)
    {
        if (target == null) return;

        _logger.LogInformation("お気に入りから削除します。Path: {path}", target.Path);

        if (FavoriteList.Contains(target))
        {
            FavoriteList.Remove(target);
            _storage.SaveJson(_appSettings);
        }
    }

}
