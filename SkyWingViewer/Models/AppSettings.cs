using SkyWingViewer.Commons;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.Models;

public class AppSettings : JsonStorageSettingBase
{
    //仮に色々変更した際のコンバート処理用。
    public string AppSettingsVersion { get; set; } = "0.01";
    public FavoriteListSettings FavoriteList{ get; set; } = new();

}


public class FavoriteListSettings
{
    public ObservableCollection<DirectoryModel> FavoriteList { get; set; } = new();

}