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
    protected override void OnStartup(StartupEventArgs e)
    {

        //ビルダー作成
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        //TODO: 暫定。初期ページ作るなり、何か考えること
        //モデル登録
        builder.Services.AddTransient<TargetDirectory>(sp =>
        {
            return new TargetDirectory("E:\\テスト用");
        });

        //サービス層登録
        builder.Services.AddSingleton<TargetNavigationService>();
        
        //vm 登録
        builder.Services.AddTransient<AssetListViewModel>();
        builder.Services.AddTransient<TargetPathBarViewModel>();

        //ビルド
        IHost host = builder.Build();

        //生成
        TargetDirectory targetDirectory = host.Services.GetRequiredService<TargetDirectory>();

        //起動時のウィンドウ
        var mainWindow = new MainWindow();

        //vm 作成
        var assetListViewModel = host.Services.GetRequiredService<AssetListViewModel>();
        var targetPathBarViewModel = host.Services.GetRequiredService<TargetPathBarViewModel>();

        mainWindow.MainArea.DataContext = assetListViewModel;
        mainWindow.ToolBar.DataContext = targetPathBarViewModel;

        mainWindow.Show();
    }
}
