using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkyWingViewer.ViewModels.Interfaces;

public interface IStarRating
{
    public ObservableCollection<StarRateItem> Stars { get; set; }
    public int Rate { get; set; }

    public void ChangeRateCommand(int rate) { }
}
