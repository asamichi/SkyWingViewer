using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkyWingViewer.Services;


//WILL: OpenCvSharp3 による縮小でサムネイル画質や速度面で改善があるか試す。https://koshian2.hatenablog.jp/entry/2017/11/23/212813

//TODO: 重いディスクアクセスを管理するサービスを別途作成すること。ディスクサービス側は常にディスクIOに専念、読み終わったらたむネイルサービスに渡すことで、なるべくシーケンシャルかつディスクの性能を使い切ることを目指す
public class ThumbnailRequest
{
    public ImageAsset Asset { get; init; }

    //TaskCompletionSource よりも Action でコールバックさせるほうが軽い。呼び出し元で後続の処理など無いのでこちらに変更
    //public TaskCompletionSource<BitmapImage> Completion { get; init; }
    public Action<BitmapImage> OnCompleted { get; }

    public CancellationToken _token;

    public ThumbnailRequest(ImageAsset asset,Action<BitmapImage> onCompleted, CancellationToken token)
    {
        Asset = asset;
        //Completion = new();
        OnCompleted = onCompleted;
        _token = token;
    }
}

public class ThumbnailService : BackgroundService
{
    ILogger _logger;
    //キー（拡張子）は大文字小文字を区別しない
    private readonly Dictionary<string, IThumbnailProvider> _providers = new Dictionary<string, IThumbnailProvider>(StringComparer.OrdinalIgnoreCase);
    public ThumbnailService(IEnumerable<IThumbnailProvider> providers,ILogger<ThumbnailService> logger)
    {
        _logger = logger;

        //重複チェックしつつ、SupportedExtensions,provider の組を登録していく。重複は一旦先勝ち
        foreach (IThumbnailProvider provider in providers)
        {
            foreach(string ext in provider.SupportedExtensions)
            {
                if (_providers.ContainsKey(ext) == false)
                {
                    _providers.Add(ext, provider);
                }
            }
        }
    }

    /* ここからバックグラウンドサービスとしての処理 */

    private readonly Channel<ThumbnailRequest> _channel = Channel.CreateBounded<ThumbnailRequest>(new BoundedChannelOptions(capacity: 300)
    {
        //FullMode = BoundedChannelFullMode.Wait,
        FullMode = BoundedChannelFullMode.DropOldest,
    });

    public async Task AddQueueAsync(ThumbnailRequest request)
    {
        try
        {
            await _channel.Writer.WriteAsync(request, request._token);
        }
        catch (ChannelClosedException)
        {
            // キャンセルされた場合は想定内なので無視して良い
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("操作がキャンセルされました。{FilePath}", request.Asset.AssetPath);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("AddQueueAsync の際に例外が発生しました。{ex}", ex);
        }

    }

    //TODO: ターゲットディレクトリが変更された際の動作の調整。中身をクリアするか、今のディレクトリを優先するようにする等。
    //TODO: キャッシュファイルの有効性チェック。元画像の最終更新日時とサムネイルの作成日時で比較
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        //ここでサムネイルサービスの並列数を指定
        //TODO: 対象ディレクトリが SSD か判別して、SSD なら並列数を増やすようにするともっと良さそう。今は HDD に併せた最適化の結果 1 並列
        using SemaphoreSlim semaphore = new SemaphoreSlim(1);
        
        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(token))
            {
                //キャンセル済みなら次に行く
                if (request._token.IsCancellationRequested)
                {
                    _logger.LogTrace("ループ進入時にキャンセル済みなためスキップされました。{FilePath}", request.Asset.AssetPath);
                    continue;
                }

                try
                {
                    //セマフォ取れるまでここで待機
                    await semaphore.WaitAsync(request._token);
                }
                catch (OperationCanceledException)
                {
                    // キャンセルされた場合は想定内なので無視して良い
                    _logger.LogTrace("セマフォ取得待ち中にキャンセルされました。{FilePath}", request.Asset.AssetPath);
                    continue;
                }

                //ここで別スレッドに処理投げる。
                //セマフォのリリースもあるので、これはリクエストのキャンセルトークンを渡さない。アプリ終了時はキャンセルで良い。
                _ = Task.Run(() =>
                {
                    try
                    {
                        string path = request.Asset.AssetPath;

                        BitmapImage bitmap = getImageCache(path);

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            request.OnCompleted.Invoke(bitmap);
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Task.Run の中で例外が発生しました。{ex}", ex);

                    }
                    finally
                    {
                        try
                        {
                            //終わったらセマフォを解放する
                            semaphore.Release();
                        }
                        catch (ObjectDisposedException)
                        {
                            // すでに Dispose されているなら、解放する必要もないので無視
                        }

                    }
                });
                
            }
        }
        catch (TaskCanceledException)
        {
            // キャンセルされた場合は想定内なので無視して良い
        }
        catch (Exception ex)
        {
            _logger.LogInformation("foreach 内部でキャッチできない例外が発生しました。{ex}", ex);

        }
    }



    /* ここからサムネイルの取得処理関係 */

    public string ThumbnailFilePath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "thumbnails");

    public int ThumbnailWidth { get; private set; } = 210;
    public int ThumbnailHeight { get; private set; } = 300;

    public record struct ImageSize
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ImageSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    public ImageSize CurrentThumbnailSize = new((int)(210*1.5),(int)(300* 1.5));
    //public ImageSize CurrentThumbnailSize = new((int)(2048), (int)(2048));


    public BitmapImage getImageCache(string path)
    {
        if (File.Exists(path) == false)
        {
            //TODO: 仮。本当はエラー用の画像にするなり検討
            //imagePath = Path.Combine(AppContext.BaseDirectory, "thumbnails");
        }

        //サムネイルファイルがある「はず」のパス
        string root = Path.GetPathRoot(path) ?? "";
        string relative = path.Substring(root.Length);
        string driveFolder = root.Replace(":", "").TrimEnd('\\');

        string thumbnailPath = Path.Combine(ThumbnailFilePath, driveFolder, relative);

        //サムネイルファイルが無いならサムネイルファイルを作成
        if (File.Exists(thumbnailPath) == false)
        {
            CreateThumbnailFile(path, thumbnailPath);
        }
        //TODO: 現状必ずディスクのサムネイルを読んでいるが、作成した場合はそれを直接返すほうがディスクIOの節約になる
        //この時点では必ずサムネイルファイルはあるので、それを読んで返す
        return CreateBitmapImage(thumbnailPath);


    }





    //サムネイルファイルを読み込んで、メモリに載せて返す。サムネイルファイルは軽量なので、ディスク負荷も軽微な想定。
    private BitmapImage CreateBitmapImage(string thumbnailPath)
    {
        BitmapImage bitmapImage = new();

        //画像のロックを最小にするために stream を作成、処理終了後に閉じる
        using FileStream stream = File.OpenRead(thumbnailPath);

        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //即座にメモリに展開して、画像へのアクセスは閉じる
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();

        //UI スレッドで利用できるように Freeze
        bitmapImage.Freeze();

        return bitmapImage;

    }


    //サムネイルファイルを作成して保存する
    //TODO: 読み込み、加工、保存はそれぞれ異なるメソッドに分離、内容を切り替えられるようにすること。
    private void CreateThumbnailFile(string filePath,string outputPath)
    {
        ImageSize currentThumbnailSize = CurrentThumbnailSize;//実体コピー。処理中にサムネイルサイズの指定が変わってもこの回での整合性は保たれる
        BitmapSource? original = null;


        //WQHD のスクショ(jpb) + HDD でも特に軽い印象だったので一旦これで
        //TODO: 10MB 越えの .bmp 形式の WQHD スクショは他の画像より優位に遅かったように見えたときがあった = 差異のポイントからこの読み取りが重かったと想定されるので、非同期にしてスレッド解放して上げても良いかも。ファイル読み取り非同期かはディスクIO待ち中のスレッド有効活用、DecodePixelWidth 等でメモリ節約できる
        
        //拡張子を取得　-> 対応する処理が _provider にあればそちらを実行
        string extension = Path.GetExtension(filePath);
        if (_providers.ContainsKey(extension))
        {
            original = _providers[extension].GetBitmapImage(filePath);
            _logger.LogTrace("オリジナルを読み込みました。{type}", ".clip 拡張");

        }

        //TODO: サムネイル画質荒くていいなら shellFile の優先度上げていいかも。OS キャッシュある時はそちらが早い、無いときは概ね同じくらい
        if (original == null)
        {
            original = DirectReadBitmapImage.GetBitmapImage(filePath, currentThumbnailSize.Width, currentThumbnailSize.Height);
            if (original != null)
                _logger.LogTrace("オリジナルを読み込みました。ファイル名：{filename}, 読み取りタイプ: {type}", Path.GetFileName(filePath), "通常読み取り");
        }
        if (original == null)
        {
            original = CreateBitmapImage(filePath);
            _logger.LogTrace("オリジナルを読み込みました。ファイル名：{filename}, 読み取りタイプ: {type}, 読み取りサイズ {x}x{y}", Path.GetFileName(filePath), "shellFile 読み取り",original.Width,original.Height);
        }


        if(original == null)
        {
            _logger.LogInformation("全ての読み取り処理を実行しましたが、original == null です。");
            return;
        }


        //縦サイズ、横サイズ、縦横比
        //int originalWidth = original.PixelWidth; //PixelWidthは int 型
        //int originalHeight = original.PixelHeight;

        ImageSize originalSize = new(original.PixelWidth, original.PixelHeight);
        double originalRatio = GetSizeRatio(originalSize.Width,originalSize.Height);

        //作成するサムネイルの比率
        double thumbnailRatio = GetSizeRatio(currentThumbnailSize.Width, currentThumbnailSize.Height);

        //切り取りサイズ
        ImageSize cropSize;



        /*  ここからサムネイルサイズの比率で可能な限り最大サイズの画像を、オリジナルから切り出す処理  */
        //オリジナルはサムネイルより横に長い
        if (originalRatio > thumbnailRatio)
        {
            //横に長すぎるので、縦幅はそのまま。横サイズはサムネイルの縦横比から計算
            int width = (int)(originalSize.Height * thumbnailRatio);
            int height = originalSize.Height;
            cropSize = new ImageSize(width, height);
        }
        else
        {
            int width = originalSize.Width;
            int height = (int)(originalSize.Width / thumbnailRatio);
            cropSize = new ImageSize(width, height);
        }

        //切り取る四角形の左上の座標
        int x = (originalSize.Width - cropSize.Width)/2;
        int y = (originalSize.Height - cropSize.Height)/2;

        //切り抜き
        CroppedBitmap croppedImage = new CroppedBitmap(original, new Int32Rect(x, y, cropSize.Width, cropSize.Height));


        /* サムネイルにする領域は切り抜けたので、サムネイルのサイズに縮小する */
        double scaleX = (double)currentThumbnailSize.Width / cropSize.Width;
        double scaleY = (double)currentThumbnailSize.Height / cropSize.Height;

        //https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.media.scaletransform.-ctor?view=windowsdesktop-8.0
        TransformedBitmap resizedImage = new TransformedBitmap(croppedImage, new ScaleTransform(scaleX, scaleY));
        resizedImage.Freeze();

        /* 保存する */
        //https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/graphics-multimedia/how-to-encode-and-decode-a-jpeg-image
        //https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.media.imaging.jpegbitmapencoder?view=windowsdesktop-10.0


        //TODO: .png とかが対象の時、全部 .png で保存される点を修正する。 
        JpegBitmapEncoder encoder = new();
        //品質は 85 あれば視覚的には最高品質らしい
        //https://developers.google.com/speed/docs/insights/OptimizeImages?hl=ja
        encoder.QualityLevel = 85;
        encoder.Frames.Add(BitmapFrame.Create(resizedImage));


        //上書きは決してしない。事故防止
        if(File.Exists(outputPath) == true)
        {
            return;
        }

        string directoryPath = Path.GetDirectoryName(outputPath) ?? "";
        // フォルダが存在しない場合は、親フォルダを含めて一気に作成する
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using Stream outputStream = File.Create(outputPath);
        encoder.Save(outputStream);

    }


    //画像サイズの縦横比の取得。どちらをどちらで割るというののミス防止
    private double GetSizeRatio(int width, int height)
    {
        return (double)width / (double)height;
    }

}
