using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Windows.Media.Imaging;
using static SkyWingViewer.Services.ThumbnailService;
using System.IO;
using SkyWingViewer.Services.ReadImageFileToBitmapImage;


namespace SkyWingViewer.Services;

public class ThumbnailDiskRequest
{
    //public string FilePath { get; set; }
    public FileSystemItemBase Model { get; set; }

    public CancellationToken _token;
    public ImageSize CurrentThumbnailSize { get; set; }

    //RunContinuationsAsynchronously を指定することで結果待ちが非同期になる
    public TaskCompletionSource<BitmapSource?> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ThumbnailDiskRequest(FileSystemItemBase model,CancellationToken token, ImageSize currentThumbnailSize)
    {
        //FilePath = filePath;
        Model = model;
        _token = token;
        CurrentThumbnailSize = currentThumbnailSize;
    }
}

public class ThumbnailDiskService : BackgroundService
{
    ILogger _logger;

    //キー（拡張子）は大文字小文字を区別しない
    private readonly Dictionary<string, IThumbnailProvider> _providers = new Dictionary<string, IThumbnailProvider>(StringComparer.OrdinalIgnoreCase);
    public ThumbnailDiskService(IEnumerable<IThumbnailProvider> providers, ILogger<ThumbnailDiskService> logger)
    {
        _logger = logger;

        //重複チェックしつつ、SupportedExtensions,provider の組を登録していく。重複は一旦先勝ち
        foreach (IThumbnailProvider provider in providers)
        {
            foreach (string ext in provider.SupportedExtensions)
            {
                if (_providers.ContainsKey(ext) == false)
                {
                    _providers.Add(ext, provider);
                }
            }
        }
    }

    /* ここからバックグラウンドサービスとしての処理 */

    private readonly Channel<ThumbnailDiskRequest> _channel = Channel.CreateBounded<ThumbnailDiskRequest>(new BoundedChannelOptions(capacity: 300)
    {
        //FullMode = BoundedChannelFullMode.Wait,
        FullMode = BoundedChannelFullMode.DropOldest,
    });

    public async Task<BitmapSource?> AddQueueAsync(ThumbnailDiskRequest request)
    {
        try
        {
            await _channel.Writer.WriteAsync(request, request._token);

            //結果が得られるまで await 
            return await request.Tcs.Task;
        }
        catch (ChannelClosedException)
        {
            // キャンセルされた場合は想定内なので無視して良い
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("操作がキャンセルされました。{FilePath}", request.Model.Path);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("AddQueueAsync の際に例外が発生しました。{ex}", ex);
            return null;
        }

    }



    protected override async Task ExecuteAsync(CancellationToken token)
    {
        //ここでサムネイルディスクサービスの並列数を指定
        //TODO: 対象ディレクトリが SSD か判別して、SSD なら並列数を増やすようにするともっと良さそう。今は HDD に併せた最適化の結果 1 並列
        using SemaphoreSlim semaphore = new SemaphoreSlim(1);

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(token))
            {
                //キャンセル済みなら次に行く
                if (request._token.IsCancellationRequested)
                {
                    _logger.LogTrace("ループ進入時にキャンセル済みなためスキップされました。{FilePath}", request.Model.Path);
                    request.Tcs.TrySetCanceled(request._token);
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
                    _logger.LogTrace("セマフォ取得待ち中にキャンセルされました。{FilePath}", request.Model.Path);
                    continue;
                }

                //ここで別スレッドに処理投げる。
                //セマフォのリリースもあるので、これはリクエストのキャンセルトークンを渡さない。アプリ終了時はキャンセルで良い。
                _ = Task.Run(async () =>
                {
                    try
                    {
                        BitmapSource? bitmap = await ReadImage(request.Model, request.CurrentThumbnailSize);

                        if (bitmap == null)
                        {
                            //TODO: 本来 null は無いはずだが、null の時にシェルからとりあえず何かを取得する処理を追加すること
                        }

                        //結果を返す
                        request.Tcs.TrySetResult(bitmap);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("ExecuteAsync: Task.Run の中で例外が発生しました。{ex}, {path}", ex, request.Model.Path);
                        request.Tcs.TrySetResult(null);

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
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は想定内なので無視して良い
            // アプリ終了時のキャンセルも想定内なので、TaskCanceledException の親クラスの TaskCanceledException を指定
        }
        catch (Exception ex)
        {
            _logger.LogError("foreach 内部でキャッチできない例外が発生しました。{ex}", ex);

        }
    }


    //読み取り
    private async Task<BitmapSource?> ReadImage(FileSystemItemBase model, ImageSize currentThumbnailSize)
    {
        BitmapSource? original = null;
        string filePath = model.Path;

        //WQHD のスクショ(jpb) + HDD でも特に軽い印象だったので一旦これで
        //TODO: 10MB 越えの .bmp 形式の WQHD スクショは他の画像より優位に遅かったように見えたときがあった = 差異のポイントからこの読み取りが重かったと想定されるので、非同期にしてスレッド解放して上げても良いかも。ファイル読み取り非同期かはディスクIO待ち中のスレッド有効活用、DecodePixelWidth 等でメモリ節約できる

        //拡張子を取得　-> 対応する処理が _provider にあればそちらを実行
        string extension = Path.GetExtension(filePath);

        if (_providers.ContainsKey(extension))
        {
            original = _providers[extension].GetBitmapImage(filePath);
            //WILL: 他の拡張子にも対応した際には適切にメッセージ変更。現状はこれで良い。
            _logger.LogTrace("オリジナルを読み込みました。{type}", ".clip 拡張");
        }
        //TODO: サムネイル画質荒くていいなら shellFile の優先度上げていいかも。OS キャッシュある時はそちらが早いかもしれない、無いときは概ね同じくらいに見える
        if (original == null && model is ImageAsset)
        {
            original = DirectReadBitmapImage.GetBitmapImage(filePath, currentThumbnailSize.Width, currentThumbnailSize.Height);
            //original = DirectReadBitmapImageNonResize.GetBitmapImage(filePath, currentThumbnailSize.Width, currentThumbnailSize.Height);
            if (original != null)
                _logger.LogTrace("オリジナルを読み込みました。ファイル名：{filename}, 読み取りタイプ: {type}", Path.GetFileName(filePath), "通常読み取り");
        }
        if (original == null && model is DirectoryModel)
        {
            original = await CreateBitmapFromShellFileToDirectory.GetBitmapImage(filePath);
            _logger.LogTrace("ディレクトリのアイコンを読み込みました。ディレクトリ名：{directoryname}, 読み取りタイプ: {type}, 読み取りサイズ {x}x{y}", Path.GetFileName(filePath), "shellFile 読み取り", original.Width, original.Height);
        }
        if (original == null)
        {
            original = await CreateBitmapFromShellFile.GetBitmapImage(filePath);
            _logger.LogTrace("オリジナルを読み込みました。ファイル名：{filename}, 読み取りタイプ: {type}, 読み取りサイズ {x}x{y}", Path.GetFileName(filePath), "shellFile 読み取り", original.Width, original.Height);
        }




        if (original == null)
        {
            _logger.LogWarning("全ての読み取り処理を実行しましたが、original == null です。");
            return null;
        }
        return original;
    }
}
