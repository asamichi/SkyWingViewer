using Microsoft.Extensions.Hosting;
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


public class ThumbnailRequest
{
    public ImageAsset Asset { get; init; }

    //TaskCompletionSource よりも Action でコールバックさせるほうが軽い。呼び出し元で後続の処理など無いのでこちらに変更
    //public TaskCompletionSource<BitmapImage> Completion { get; init; }
    public Action<BitmapImage> OnCompleted { get; }

    public ThumbnailRequest(ImageAsset asset,Action<BitmapImage> onCompleted)
    {
        Asset = asset;
        //Completion = new();
        OnCompleted = onCompleted;
    }
}

public class ThumbnailService : BackgroundService
{
    private readonly Channel<ThumbnailRequest> _channel = Channel.CreateBounded<ThumbnailRequest>(new BoundedChannelOptions(capacity: 100)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });

    public async Task AddQueueAsync(ThumbnailRequest request, CancellationToken token)
    {
        try
        {
            await _channel.Writer.WriteAsync(request, token);
        }
        catch (ChannelClosedException)
        {
            // キャンセルされた場合は想定内なので無視して良い
        }
        catch (Exception ex)
        {
            //TODO: 例外をログに記録
        }

    }

    //TODO: ターゲットディレクトリが変更された際の動作の調整。中身をクリアするか、今のディレクトリを優先するようにする等。
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        //ここでサムネイルサービスの並列数を指定
        using SemaphoreSlim semaphore = new SemaphoreSlim(4);
        
        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(token))
            {
                //セマフォ取れるまでここで待機
                await semaphore.WaitAsync(token);

                //ここで別スレッドに処理投げる
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
                    catch (TaskCanceledException)
                    {
                        // キャンセルされた場合は想定内なので無視して良い
                    }
                    catch (Exception ex)
                    {
                        //TODO: 例外をログに記録
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
                },token);
                
            }
        }
        catch (Exception ex)
        {
            //TODO: 例外のログ出力
        }
    }




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

    public ImageSize CurrentThumbnailSize = new(210, 300);





    public BitmapImage getImageCache(string path)
    {
        if(File.Exists(path) == false)
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
            CreateThumbnailFile(path,thumbnailPath);
        }
        //この時点では必ずサムネイルファイルはあるので、それを読んで返す
        return CreateBitmapImage(thumbnailPath);


    }



    //サムネイルファイルを読み込んで、メモリに載せて返す。サムネイルファイルは軽量なので、ディスク負荷も軽微な想定。
    //TODO: 呼び出し元でそもそもしっかりするべきだが、一応 path 先の null チェック追加。
    private BitmapImage CreateBitmapImage(string thumbnailPath)
    {
        BitmapImage bitmapImage = new();

        //画像のロックを最小にするために stream を作成、処理終了後に閉じる
        using FileStream stream = File.OpenRead(thumbnailPath);

        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; //即座にメモリに展開して、画像へのアクセスは閉じる
        bitmapImage.StreamSource = stream;
        bitmapImage.DecodePixelWidth = 210;
        bitmapImage.EndInit();

        //UI スレッドで利用できるように Freeze
        bitmapImage.Freeze();

        return bitmapImage;

    }


    //サムネイルファイルを作成して保存する
    private void CreateThumbnailFile(string filePath,string outputPath)
    {
        ImageSize currentThumbnailSize = CurrentThumbnailSize;//実体コピー。処理中にサムネイルサイズの指定が変わってもこの回での整合性は保たれる
        BitmapImage original = new();


        //WQHD のスクショ(jpb) + HDD でも特に軽い印象だったので一旦これで
        //TODO: 10MB 越えの .bmp 形式の WQHD スクショは他の画像より優位に遅かったように見えたときがあった = 差異のポイントからこの読み取りが重かったと想定されるので、非同期にしてスレッド解放して上げても良いかも。ファイル読み取り非同期かはディスクIO待ち中のスレッド有効活用、DecodePixelWidth 等でメモリ節約できる

        using(FileStream stream = File.OpenRead(filePath))
        {
            original.BeginInit();
            original.CacheOption = BitmapCacheOption.OnLoad;
            original.StreamSource = stream;
            //サイズ自体は暫定。簡易的な最適化として
            original.DecodePixelWidth = currentThumbnailSize.Width * 2;
            original.EndInit();
            original.Freeze();
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
