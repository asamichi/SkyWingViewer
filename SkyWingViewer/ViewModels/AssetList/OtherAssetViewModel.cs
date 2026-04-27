using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Shell;
using SkyWingViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace SkyWingViewer.ViewModels;

//TODO: とりあえず表示という形で全て暫定処理。
public partial class OtherAssetViewModel : AssetViewModelBase<OtherAsset>
{

    [ObservableProperty]
    private BitmapSource? iconImage;

    public  CancellationToken _cancellationToken;
    private ILogger<OtherAssetViewModel> _logger;


    public OtherAssetViewModel(OtherAsset otherAsset, CancellationToken ct,ILogger<OtherAssetViewModel> logger) : base(otherAsset)
    {
        _logger = logger;
        _cancellationToken = ct;
        _ = Task.Run(async () =>
        {
            //await GetIconAsync(otherAsset.AssetPath);
            await LoadThumbnail();
        },_cancellationToken);
    }


    //TODO: 通常アイコンはこちらの方がきれい
    public async Task LoadThumbnail()
    {
        // すでに読み込み済みなら何もしない
        if (IconImage != null) return;

        _logger.LogTrace("アイコンの取得を開始します。Path: {path}", _asset.AssetPath);

        // 重い処理（Shellアクセス）を別スレッドで実行
        IconImage = await Task<BitmapSource?>.Run(() =>
        {
            try
            {

                using (var shellFile = ShellFile.FromFilePath(_asset.AssetPath))
                {
                    BitmapSource bitmap = shellFile.Thumbnail.BitmapSource;
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch(Exception ex)
            {
                _logger.LogInformation("LoadThumbnail にてエラーが発生しました : {ex}",ex);
                return null; // 失敗時はnull（XAML側でデフォルトアイコンを表示させるのが楽）
            }
        });
        return;
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

        if (bitmap != null) IconImage = bitmap;
    }


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
