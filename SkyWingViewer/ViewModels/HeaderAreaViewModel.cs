using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.ViewModels;

public class HeaderAreaViewModel
{
    private IServiceProvider _serviceProvider;
    public TargetPathBarViewModel TargetPathBarViewModel { get; set; }
    public SearchBarViewModel SearchBarViewModel { get; set; }

    public SortAreaViewModel SortAreaViewModel { get; set; }

    public HeaderAreaViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        TargetPathBarViewModel = _serviceProvider.GetRequiredService<TargetPathBarViewModel>();
        SearchBarViewModel = _serviceProvider.GetRequiredService<SearchBarViewModel>();
        SortAreaViewModel = _serviceProvider.GetRequiredService<SortAreaViewModel>();
    }


    
    
}
