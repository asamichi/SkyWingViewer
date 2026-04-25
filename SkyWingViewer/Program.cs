using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Serilog;
using SkyWingViewer.Commons;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using SkyWingViewer.ViewModels;
using SkyWingViewer.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Threading;



namespace SkyWingViewer;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {

        /* *************** Host の設定～起動 ************** */
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        /* **********
         * ログ関係
        /* serilog の設定 
         * CompactJsonFormatter 形式のログなため、閲覧時には下記等を DL して利用することを推奨
         https://github.com/warrenbuckley/Compact-Log-Format-Viewer/releases
         
         * **********/
        //設定ファイル読み込み
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // アプリのディレクトリを基準パスに設定
            .AddJsonFile("appsettings.json")  // 設定ソース(1): JSONファイル
            .Build();

        //  読み込んだ設定ファイルを元に設定
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        // 既存のプロバイダをクリア
        builder.Logging.ClearProviders();
        //ILogger<T>でSerilogが使えるようにDIに登録（Log.Loggerの設定を使用）
        builder.Logging.AddSerilog(dispose: true);
        /* ********** ログ関係 ここまで ********** */


        //TODO: 暫定。初期ページ作るなり、何か考えること
        /* ********** モデル登録 ********* */
        builder.Services.AddTransient<TargetDirectory>(sp =>
        {
            return new TargetDirectory("E:\\テスト用");
        });

        //json ストレージ
        builder.Services.AddSingleton<JsonStorage<AppSettings>>(sp =>
        {
            return new JsonStorage<AppSettings>("UserSettings.json");
        });

        /* ********** サービス層登録 ********* */
        //ターゲットディレクトリのパス
        builder.Services.AddSingleton<TargetNavigationService>();

        //サムネイル関係
        builder.Services.AddSingleton<ThumbnailService>();
        builder.Services.AddHostedService<ThumbnailService>(sp=> sp.GetRequiredService<ThumbnailService>());

        //サムネイル関係(拡張子追加)
        builder.Services.AddSingleton<IThumbnailProvider, ClipStudioThumbnailLoader>();

        //設定
        builder.Services.AddSingleton<AppSettings>(sp =>
        {
            return sp.GetRequiredService<JsonStorage<AppSettings>>().LoadJson();
        });
        //各種機能で使う設定だけ読み込む際に必要なので、設定クラスそれぞれについても依存性を注入すること
        builder.Services.AddSingleton<FavoriteListSettings>(sp =>
        {
            return sp.GetRequiredService<AppSettings>().FavoriteList;
        });

        //お気に入り
        builder.Services.AddSingleton<FavoriteListService>();

        //各アセットかディレクトリの詳細情報
        builder.Services.AddSingleton<ItemInformationService>();
           
        /* ********** vm 登録 ********* */
        //画面というか領域
        builder.Services.AddTransient<AssetListViewModel>();
        builder.Services.AddTransient<TargetPathBarViewModel>();
        builder.Services.AddTransient<FavoriteListViewModel>();
        builder.Services.AddTransient<AssetInformationViewModel>();

        //一覧の単体
        builder.Services.AddTransient<ImageAssetViewModel>();
        builder.Services.AddTransient<OtherAssetViewModel>();
        builder.Services.AddTransient<DirectoryViewModel>();

        //その他
        builder.Services.AddSingleton<AssetListViewModelFactory>();


        //ビルド
        IHost host = builder.Build();
        /* *************** Host の設定～起動 ここまで ************** */

        //生成
        TargetDirectory targetDirectory = host.Services.GetRequiredService<TargetDirectory>();

        //起動時のウィンドウ
        var mainWindow = new MainWindow();

        //vm 作成
        var assetListViewModel = host.Services.GetRequiredService<AssetListViewModel>();
        var targetPathBarViewModel = host.Services.GetRequiredService<TargetPathBarViewModel>();
        var favoriteListViewModel = host.Services.GetRequiredService<FavoriteListViewModel>();
        var assetInformationViewModel = host.Services.GetRequiredService<AssetInformationViewModel>();

        mainWindow.MainArea.DataContext = assetListViewModel;
        mainWindow.ToolBar.DataContext = targetPathBarViewModel;
        mainWindow.TreeMenu.DataContext = favoriteListViewModel;
        mainWindow.SubArea.DataContext = assetInformationViewModel;

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
