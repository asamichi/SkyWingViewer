using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;

//Popup の表示対象となる ViewModel のためのインタフェース
//呼び出し元の ViewModel を保持するプロパティの存在を保証する
public interface IPopupControl
{
    //実装が無いなら何もしない。実装は強制しない。
    public void OnPopupClosed() { }
}
