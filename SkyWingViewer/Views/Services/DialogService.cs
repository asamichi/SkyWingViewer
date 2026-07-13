using Microsoft.Win32;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SkyWingViewer.Views.Services;

//TODO: メッセージボックスもこちらに実装を移行
//TODO: 削除など注意が必要な操作について、削除と入力しないといけないような UI 等、注意が必要かつ頻度が低い想定の操作にひと手間を加える
public class DialogService : IDialogService
{
    public DialogService()
    {
    }

    public string? OpenFolderDialog(DialogServiceOptions option)
    {
        var dialog = new OpenFolderDialog
        {
            Title = option.Title,
            InitialDirectory = option.InitialDirectory,
        };

        if (dialog.ShowDialog() == true)
        {
            //フォルダが選択された場合
            string selectedPath = dialog.FolderName;
            return selectedPath;
        }

        return null;
    }

    public bool OpenMessageBox(DialogServiceOptions option)
    {
        // メッセージボックスで確認する
        MessageBoxResult result = MessageBox.Show(
            //メッセージ
            option.Message,
            //メッセージボックスのタイトル
            option.Title,
            //ボタンの種類の指定
            option.MessageBoxButton,
            // アイコンの種類（警告）
            option.MessageBoxImage
        );

        //OK,はい,リトライ,続行 が肯定
        if (result == MessageBoxResult.Yes || result == MessageBoxResult.OK || result == MessageBoxResult.Continue || result == MessageBoxResult.Retry)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
}
