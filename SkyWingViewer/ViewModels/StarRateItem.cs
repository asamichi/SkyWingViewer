using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public partial class StarRateItem : ObservableObject
{
    [ObservableProperty]
    private bool isFilled = false;

    //何番目の星か
    public int StarIndex { get; set; }

    public StarRateItem(int starIndex)
    {
        StarIndex = starIndex;
    }
}