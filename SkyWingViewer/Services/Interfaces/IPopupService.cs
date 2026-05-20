using SkyWingViewer.ViewModels;
using SkyWingViewer.Views.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;


//Popup を生成するサービスへの入り口
public interface IPopupService
{
    public void CreatePopup(IPopupControl VM, Object targetPlaceElement, PopupServiceOptions? options = null);
}
