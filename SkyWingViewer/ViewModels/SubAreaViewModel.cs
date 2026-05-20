using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class SubAreaViewModel
{
    private IServiceProvider _serviceProvider;

    public AssetInformationViewModel AssetInformationViewModel { get; set; }
    public InformationTagViewModel InformationTagViewModel { get; set; }

    public SubAreaViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        AssetInformationViewModel = _serviceProvider.GetRequiredService<AssetInformationViewModel>();
        InformationTagViewModel = _serviceProvider.GetRequiredService<InformationTagViewModel>();
    }
}
