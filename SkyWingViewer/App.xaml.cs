using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyWingViewer.Models;
using SkyWingViewer.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

using SkyWingViewer.Views;

namespace SkyWingViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {

        //TODO: host 等設定すること。一旦仮でこの実装
        var mainWindow = new MainWindow();

        var assetListViewModel = new AssetListViewModel();

        //TODO: 暫定、テスト用。Pathのバーを追加して、そちらを参照するようにすること
        assetListViewModel.LoadDirectory("E:\\スクショ");

        mainWindow.DataContext = assetListViewModel;

        mainWindow.Show();
    }
}
