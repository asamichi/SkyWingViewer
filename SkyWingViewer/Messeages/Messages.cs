using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.Messeages;

//public record ChangeUpdateSelectionOnAssetList(ObservableCollection<Object> assets);
//public record ChangeTargetInfomation(List<ItemInformation> InformationList);
public record EditTagOnPopup(string tagName,bool isAdd);