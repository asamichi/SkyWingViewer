using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Shell;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace SkyWingViewer.ViewModels;

//TODO: とりあえず表示という形で全て暫定処理。
public partial class OtherAssetViewModel : AssetViewModelBase<OtherAsset>
{
    public  CancellationToken _cancellationToken;
    private ILogger<OtherAssetViewModel> _logger;

    //同じファイルのサムネイル処理を複数回しないように
    private int _isLoading = 0;
    private int _isVisible = 0;
    public CancellationTokenSource _childCancellationTokenSource;
    private ThumbnailService thumbnailService;

    //このインスタンスをデータコンテキストとしている View の数
    //折り返し部分などで、自分にとっては _lastVM だが、別の View が今まさに表示しているという場合があることへの対策。 == 0 の時のみサムネイルを解放
    public int ViewCount { get; set; } = 0;



    public OtherAssetViewModel(OtherAsset otherAsset, CancellationToken ct,ILogger<OtherAssetViewModel> logger, ThumbnailService ts) : base(otherAsset)
    {
        _logger = logger;
        _cancellationToken = ct;
        thumbnailService = ts;

        //_ = Task.Run(async () =>
        //{
        //    try
        //    {
        //        //await GetIconAsync(otherAsset.AssetPath);
        //        await LoadThumbnail();
        //    }
        //    catch(Exception ex)
        //    {
        //        _logger.LogError("OtherAssetViewModel コンストラクタの中で例外が発生しました。{ex}", ex);
        //    }

        //},_cancellationToken);

        //_ = LoadThumbnail();
        //_ = GetIconAsync(_model.Path);
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
                _logger.LogTrace("再度サムネイル作成要求がありました。。Path: {Path}", _asset.AssetPath);

                return;
            }

            _isLoading = 1;
            //Thumbnail = await Task.Run(() => ts.getImageCache(_asset.AssetPath));
            //Thumbnail = ts.getImageCache(_asset.AssetPath);


            _childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            _logger.LogTrace("サムネイルの作成リクエストを実施します。Path: {Path}", _asset.AssetPath);
            ThumbnailRequest thumbnailRequest = new ThumbnailRequest(_asset, async (result) =>
            {
                if (this._isVisible == 0)
                {
                    _logger.LogTrace("既に必要のないサムネイルのため、値を格納しません。Path: {Path}", _asset.AssetPath);
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

        _logger.LogTrace("サムネイルを解放します。Path: {Path}", _asset.AssetPath);
        _isVisible = 0;
        Thumbnail = null;

        _childCancellationTokenSource?.Cancel();
        _childCancellationTokenSource?.Dispose();
    }


    //TODO url はこちらで無いと読めない
    private async Task GetIconAsync(string path)
    {
        ////ファイルに格納されているアイコンか、ファイルに関連付けられている実行ファイルのアイコンを持ってくる
        //using (Icon? icon = Icon.ExtractAssociatedIcon(path))
        //{
        //    if (icon == null)
        //    {
        //        //TODO icon が取得できない時は専用の icon を渡すなりする
        //        return;
        //    }
        //    // System.Drawing.Icon を WPF 用の ImageSource に変換
        //    IconImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
        //        icon.Handle,
        //        System.Windows.Int32Rect.Empty,
        //        BitmapSizeOptions.FromEmptyOptions());
        //    IconImage.Freeze();
        //}

        //if (IconImage != null) return;


        //AI: AI に作らせたのコピペ。アイコンの取得。上記処理では steam のゲームのショートカットアイコン（URL）を持ってこれなかった
        var bitmap = await Task.Run(() =>
        {
            // 1. まずは普通に取ってみる
            var icon = SteamIconResolver.GetFullShellIcon(path);

            // 2. 地球儀（またはnull）なら、中身を直接解析する
            if (icon == null || path.EndsWith(".url"))
            {
                try
                {
                    var content = File.ReadAllLines(path);
                    var iconLine = content.FirstOrDefault(l => l.StartsWith("IconFile="));
                    if (iconLine != null)
                    {
                        var iconPath = iconLine.Split('=')[1].Trim();
                        // 中に書いてある .ico ファイルから直接抜く
                        return SteamIconResolver.GetFullShellIcon(iconPath);
                    }
                }
                catch { /* ファイルが読み込めない場合など */ }
            }
            return icon;
        });

        if (bitmap != null) Thumbnail = bitmap;
    }

    //TODO: steam ショートカットのアイコンが表示できるように .url はこれで読むようにプラグインに移植する
    //AI: AI に作らせたのコピペ。
    public static class SteamIconResolver
    {
        // Win32 API / COM の最小定義
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO { public IntPtr hIcon; public int iIcon; public uint dwAttributes; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static BitmapSource? GetFullShellIcon(string path)
        {
            // Steam用フラグ: 
            // SHGFI_ICON (0x100) | SHGFI_ADDOVERLAYS (0x000000020)
            // 0x10 (USEFILEATTRIBUTES) は絶対に入れない
            uint flags = 0x100 | 0x000000020;

            var shfi = new SHFILEINFO();
            // ここで 0 を渡さず、パスを直接解析させる
            IntPtr res = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (shfi.hIcon == IntPtr.Zero) return null;

            try
            {
                var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    shfi.hIcon,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }
    }

}
