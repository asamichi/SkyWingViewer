using Microsoft.Extensions.Logging;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SkyWingViewer.Views.Services;

public class WindowService : IWindowService
{
    private ILogger _logger;

    public WindowService(ILogger<WindowService> logger)
    {
        _logger = logger;
    }

    public void CreateWindow(IWindowViewModel VM,WindowServiceOptions options)
    {
        Window newWindow = new Window
        {
            Title = options.Title,
            SizeToContent = options.SizeToContent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = VM,
        };

        if(options.Width is double width)
        {
            newWindow.Width = width;
        }
        if(options.Height is double height)
        {
            newWindow.Height = height;
        }

        if(options.IsMaximized == true)
        {
            newWindow.WindowState = WindowState.Maximized;
        }

        ContentPresenter contentPresenter = new ContentPresenter
        {
            Content = VM
        };

        newWindow.Content = contentPresenter;

        if(VM.GetType().GetProperty("Title") != null)
        {
            newWindow.SetBinding(Window.TitleProperty, new Binding("Title"));
        }


        newWindow.Show();
    }
}
