using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
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
using SkyWingViewer.Views.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

//TODO: タグ・レートともに、複数を対象に編集するときに確認するようにしても良さそう。要件等

namespace SkyWingViewer;

//TODO: 文字入力系について、Enter かフォーカス外れたら発火するようにする。
class Program
{
    [STAThread]
    static void Main(string[] args)
    {

        //諸々終わったらアプリケーション起動
        var app = new App();
        app.InitializeComponent();

        /* *************** Host の設定～起動 ************** */
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();


        /* **********
         * DB 関係の設定
         * **********/
        //appsettings.json に書くこともできる。今回は直接書いてしまう
        var connectionString = builder.Configuration.GetConnectionString("SqliteConnection")
                               ?? "Data Source=Database/SkyWingViewer.db";
        // 「Data Source=」の部分を削ってパスだけにする
        var rawPath = connectionString.Replace("Data Source=", "");

        // フォルダ部分（Database/）を取り出して、なければ作成する
        var directory = Path.GetDirectoryName(rawPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        //SQLite
        builder.Services.AddDbContext<MyDbContext>(options =>
            options.UseSqlite(
                builder.Configuration.GetConnectionString("SqliteConnection")));


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
            //return new TargetDirectory("E:\\テスト用");
            return new TargetDirectory(AppContext.BaseDirectory);
        });

        //json ストレージ
        builder.Services.AddSingleton<JsonStorage<AppSettings>>(sp =>
        {
            return new JsonStorage<AppSettings>("UserSettings.json");
        });

        /* ********** サービス層登録 ********* */

        //メッセンジャー
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        //ターゲットディレクトリのパス
        builder.Services.AddSingleton<TargetNavigationService>();

        builder.Services.AddSingleton<ItemSearchService>();
        builder.Services.AddSingleton<ItemSortService>();

        //メイン画面のアセット一覧管理
        builder.Services.AddSingleton<AssetListService>();
        builder.Services.AddSingleton<AssetSelectionService>();

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

        //タグ関係
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<ItemTagService>();

        //Popup 生成サービス
        builder.Services.AddSingleton<PopupService>();
        builder.Services.AddTransient<IPopupService>(sp => sp.GetRequiredService<PopupService>());

        //星
        builder.Services.AddSingleton<StarRatingService>();

        //メモ
        builder.Services.AddSingleton<MemoService>();

        /* ********** vm 登録 ********* */
        //画面というか領域
        //ヘッダー
        builder.Services.AddTransient<TargetPathBarViewModel>();
        builder.Services.AddTransient<SearchBarViewModel>();
        builder.Services.AddTransient<HeaderAreaViewModel>();
        builder.Services.AddTransient<SortAreaViewModel>();
        builder.Services.AddTransient<FilterButtonAreaViewModel>();
        builder.Services.AddTransient<StarRatingFilterViewModel>();


        //左
        builder.Services.AddTransient<SideMenuAreaViewModel>();
        builder.Services.AddTransient<FavoriteListViewModel>();

        //メイン
        builder.Services.AddTransient<AssetListViewModel>();
        //一覧のファクトリー
        builder.Services.AddSingleton<AssetListViewModelFactory>();
        //一覧の単体
        //builder.Services.AddTransient<ImageAssetViewModel>();
        //builder.Services.AddTransient<OtherAssetViewModel>();
        //builder.Services.AddTransient<DirectoryViewModel>();

        //右
        builder.Services.AddTransient<SubAreaViewModel>();
        builder.Services.AddTransient<AssetInformationViewModel>();
        builder.Services.AddTransient<InformationTagViewModel>();
        builder.Services.AddTransient<StarRatingViewModel>();
        builder.Services.AddTransient<MemoViewModel>();

        //Popup
        builder.Services.AddTransient<TagEditViewModel>();
        builder.Services.AddTransient<TagSearchViewModel>();
        builder.Services.AddTransient<TagListViewModel>();


        //ビルド
        IHost host = builder.Build();
        /* *************** Host の設定～起動 ここまで ************** */

        //生成
        TargetDirectory targetDirectory = host.Services.GetRequiredService<TargetDirectory>();

        //起動時のウィンドウ
        var mainWindow = new MainWindow();

        //vm 作成
        var assetListViewModel = host.Services.GetRequiredService<AssetListViewModel>();
        var headerAreaViewModel = host.Services.GetRequiredService<HeaderAreaViewModel>();
        var sideMenuAreaViewModel = host.Services.GetRequiredService<SideMenuAreaViewModel>();
        var subAreaViewModel = host.Services.GetRequiredService<SubAreaViewModel>();

        mainWindow.MainArea.DataContext = assetListViewModel;
        mainWindow.ToolBar.DataContext = headerAreaViewModel;
        mainWindow.TreeMenu.DataContext = sideMenuAreaViewModel;
        mainWindow.SubArea.DataContext = subAreaViewModel;

        //mainWindow.Show();


        // 諸々処理。await はせず、下記のような感じでサービスは起動
        /*
        host.Start() は host.StartAsync().GetAwaiter().GetResult(); を同期的に実行する。
        https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.extensions.hosting.hostingabstractionshostextensions.start?view=net-10.0-pp&viewFallbackFrom=net-8.0#microsoft-extensions-hosting-hostingabstractionshostextensions-start(microsoft-extensions-hosting-ihost)
        */
        host.StartAsync().GetAwaiter().GetResult(); //サービスの起動について await 、起動が完了したら進む

        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            //状況を確認し、C# 側と DB 側で差異があればカラムの追加などを実施する
            dbContext.Database.Migrate();

            //WAL モードで起動する
            //dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        app.Run(mainWindow); // アプリ起動中はこの行で止まる

        //アプリが終了されたらサービスも片付けて終了
        host.StopAsync().GetAwaiter().GetResult();


    }
}
