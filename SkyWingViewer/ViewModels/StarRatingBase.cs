using CommunityToolkit.Mvvm.ComponentModel;
using SkyWingViewer.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SkyWingViewer.ViewModels;

public abstract partial class StarRatingBase : ObservableObject, IStarRating
{
    [ObservableProperty]
    private ObservableCollection<StarRateItem> stars = new();

    //public ObservableCollection<StarRateItem> Stars { get; set; } = new();
    public int Rate { get; set; } = 0;

    public StarRatingBase()
    {
        for (int i = 1; i <= 5; i++)
        {
            Stars.Add(new StarRateItem(i));
        }
    }
    public void UpdateStars()
    {
        foreach (var star in Stars)
        {
            if (star.StarIndex <= Rate)
            {
                star.IsFilled = true;
            }
            else
            {
                star.IsFilled = false;
            }
        }
    }
}
