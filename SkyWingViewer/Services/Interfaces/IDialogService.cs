using System;
using System.Collections.Generic;
using System.Text;
using SkyWingViewer.Views.Services;

namespace SkyWingViewer.Services;


//ダイアログ生成サービスへの入り口
public interface IDialogService
{
    public string? OpenFolderDialog(DialogServiceOptions option);
    public bool OpenMessageBox(DialogServiceOptions option);
}


