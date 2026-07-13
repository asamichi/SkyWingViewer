using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SkyWingViewer.Services;

//メッセージボックス
// https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/windows/how-to-open-message-box
//メッセージボックスのアイコン
//https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.messageboximage?view=windowsdesktop-10.0

public class DialogServiceOptions
{
    public string? Title { get; set; } = null;
    public string? InitialDirectory { get; set; } = null;

    public string? Message { get; set; } = null;

    public MessageBoxButton MessageBoxButton { get; set; } = MessageBoxButton.OKCancel;

    public MessageBoxImage MessageBoxImage { get; set; } = MessageBoxImage.Information;

}


/*
 例置き場
## フォルダ選択
         DialogServiceOptions dialogOptions = new DialogServiceOptions();
        dialogOptions.Title = "移動したライブラリフォルダを選択してください（先にエクスプローラー等でライブラリフォルダを移動後、本操作から移動後のライブラリフォルダを指定してください）";
        dialogOptions.InitialDirectory = _targetNavigationService.Path;
## YesNo

 
 */