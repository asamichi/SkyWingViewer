using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyWingViewer.Models;
using SkyWingViewer.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

using SkyWingViewer.Services;
using SkyWingViewer.Views;

namespace SkyWingViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    //Program.cs から起動する場合に、App.xaml の内容を反映するため
    public App()
    {
        InitializeComponent();
    }

}
