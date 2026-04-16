using System;
using System.Collections.Generic;
using System.Text;
using SkyWingViewer.Commons;
using SkyWingViewer.Services;

namespace SkyWingViewer.Models;

public class AppSettings : JsonStorageSettingBase
{
    //仮に色々変更した際のコンバート処理用。
    public string AppSettingsVersion { get; set; } = "0.01";


}


public class FavoriteListSettings
{
    public List<DirectoryModel> FavoriteList { get; set; } = new();

}