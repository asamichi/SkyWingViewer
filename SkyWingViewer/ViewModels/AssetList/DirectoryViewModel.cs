using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Shell;
using NetTopologySuite.Utilities;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.RightsManagement;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace SkyWingViewer.ViewModels;

public partial class DirectoryViewModel : FileSystemItemViewModelBase<DirectoryModel>
{
    //モデルとディレクトリパス
    public string directoryPath => _model.Path;

    private ILogger<DirectoryViewModel> _logger;

    //ディレクトリ遷移のためのサービス
    private TargetNavigationService _targetNavigationService;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private int _isVisible = 0;
    public CancellationToken _cancellationToken;
    public CancellationTokenSource _childCancellationTokenSource;
    private ThumbnailService thumbnailService;


    //このインスタンスをデータコンテキストとしている View の数
    //折り返し部分などで、自分にとっては _lastVM だが、別の View が今まさに表示しているという場合があることへの対策。 == 0 の時のみサムネイルを解放
    public int ViewCount { get; set; } = 0;

    public DirectoryViewModel(DirectoryModel directory,ILogger<DirectoryViewModel> logger, TargetNavigationService targetNavigationService,ThumbnailService ts) : base(directory)
    {
        _logger = logger;
        _targetNavigationService = targetNavigationService;
        thumbnailService = ts;

        //_ = Task.Run(async () =>
        //{
        //    try
        //    {
        //        await GetIconAsync(directoryPath);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("DirectoryViewModel コンストラクタの中で例外が発生しました。{ex}", ex);
        //    }
        //});

        OpenCommand = DirectoryOpenCommand;
    }


    private async Task GetIconAsync(string path)
    {
        _logger.LogTrace("アイコンの取得を開始します。Path: {path}", path);

        //TODO: アイコンの取得もちゃんと並列化したい。一旦仮で不便ない程度の実装
        Thumbnail = await Task<BitmapSource?>.Run(() =>
        {
            try
            {
                using (var shellFolder = ShellContainer.FromParsingName(path))
                {
                    // アイコンを取得。サイズは Large, Medium, Small など選べる。
                    BitmapSource bitmap = shellFolder.Thumbnail.BitmapSource;
                    bitmap.Freeze();
                    _logger.LogTrace("アイコンの取得が完了しました。Path : {path}", path);
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("GetIconAsync にてエラーが発生しました : {ex}", ex);
                //TODO:  エラー時はデフォルトアイコンなりを返す
                return null;
            }
        }
        );
    }


    //WILL: ImageAssetViewModel からコピペ。今後も処理共通路線で確定なら基底クラスに移植を検討
    public async Task LoadThumbnail()
    {
        try
        {
            _isVisible = 1;
            // すでに読み込み済みなら何もしない
            if (_isLoading == 1 || Thumbnail != null)
            {
                _logger.LogTrace("再度サムネイル作成要求がありました。。Path: {Path}", _model.Path);

                return;
            }

            _isLoading = 1;

            _childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            _logger.LogTrace("サムネイルの作成リクエストを実施します。Path: {Path}", _model.Path);
            ThumbnailRequest thumbnailRequest = new ThumbnailRequest(_model, async (result) =>
            {
                if (this._isVisible == 0)
                {
                    _logger.LogTrace("既に必要のないサムネイルのため、値を格納しません。Path: {Path}", _model.Path);
                    return;
                }
                this.Thumbnail = result;
            }, _childCancellationTokenSource.Token);
            await thumbnailService.AddQueueAsync(thumbnailRequest);

            _isLoading = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("LoadThumbnail の中で例外が発生しました。{ex}", ex);
        }

    }
    public void UnloadThumbnail()
    {

        _logger.LogTrace("サムネイルを解放します。Path: {Path}", _model.Path);
        _isVisible = 0;
        Thumbnail = null;

        _childCancellationTokenSource?.Cancel();
        _childCancellationTokenSource?.Dispose();
    }


    [RelayCommand]
    public void DirectoryOpen()
    {
        _targetNavigationService.SetPath(directoryPath);
    }

}
