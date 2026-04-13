using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.ViewModels;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;
using System.Windows;

namespace SkyWingViewer;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {

        //    //ビルダー作成
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        //TODO: 暫定。初期ページ作るなり、何か考えること
        //モデル登録
        builder.Services.AddTransient<TargetDirectory>(sp =>
        {
            return new TargetDirectory("E:\\テスト用");
        });

        //サービス層登録
        builder.Services.AddSingleton<TargetNavigationService>();

        builder.Services.AddSingleton<ThumbnailService>();
        builder.Services.AddHostedService<ThumbnailService>(sp=> sp.GetRequiredService<ThumbnailService>());

        //vm 登録
        builder.Services.AddTransient<AssetListViewModel>();
        builder.Services.AddTransient<TargetPathBarViewModel>();
        builder.Services.AddSingleton<AssetListViewModelFactory>();

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

        //mainWindow.Show();


        // 諸々処理。await はせず、下記のような感じでサービスは起動
        /*
        host.Start() は host.StartAsync().GetAwaiter().GetResult(); を同期的に実行する。
        https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.extensions.hosting.hostingabstractionshostextensions.start?view=net-10.0-pp&viewFallbackFrom=net-8.0#microsoft-extensions-hosting-hostingabstractionshostextensions-start(microsoft-extensions-hosting-ihost)
        */
        host.StartAsync().GetAwaiter().GetResult(); //サービスの起動について await 、起動が完了したら進む


        //諸々終わったらアプリケーション起動
        var app = new App();
        app.InitializeComponent();

        app.Run(mainWindow); // アプリ起動中はこの行で止まる

        //アプリが終了されたらサービスも片付けて終了
        host.StopAsync().GetAwaiter().GetResult();


    }
}
