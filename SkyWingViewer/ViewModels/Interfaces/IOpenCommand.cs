using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkyWingViewer.ViewModels;

public interface  IOpenCommand
{
    //開く、処理
    ICommand OpenCommand { get; }
}
