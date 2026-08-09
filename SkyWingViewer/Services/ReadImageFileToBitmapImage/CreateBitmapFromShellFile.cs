using Microsoft.WindowsAPICodePack.Shell;
using NetTopologySuite.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SkyWingViewer.Services;

public class CreateBitmapFromShellFile
{

    //public static async Task<BitmapSource?> GetBitmapImage(string filePath)
    //{
    //    int retryCnt = 0;
    //    using (var shellFile = ShellFile.FromFilePath(filePath))
    //    {
    //        BitmapSource? bitmap = null;
    //        while (retryCnt < 3)
    //        {
    //            bitmap = shellFile.Thumbnail.BitmapSource;
                
    //            if(bitmap != null)
    //            {
    //                break;
    //            }

    //            retryCnt++;
    //            await Task.Delay(1000);
    //        }

    //        if(bitmap == null)
    //        {
    //            return null;
    //        }

    //        bitmap.Freeze();
    //        return (BitmapSource)bitmap;
    //    }
    //}

    //OtherAssetViewModel から移植
    public static async Task<BitmapSource?> GetBitmapImage(string filePath)
    {

        int retryCnt = 0;
        int maxRetryCnt = 2;
        BitmapSource? IconImage = null;

        try
        {
            //_logger.LogTrace("アイコンの取得を開始します。Path: {path}", _asset.AssetPath);

            while (retryCnt < maxRetryCnt)
            {
                // 重い処理（Shellアクセス）を別スレッドで実行
                IconImage = await Task.Run(() =>
                {
                    try
                    {
                        using var shellFile = ShellFile.FromFilePath(filePath);
                        BitmapSource bitmap = shellFile.Thumbnail.BitmapSource;
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch (OperationCanceledException)
                    {
                        //外側のキャッチに任せる
                        throw;
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError("LoadThumbnail、Task<BitmapSource?>.Run にてエラーが発生しました : {ex} {filename}", ex, ItemPath);

                        //TODO: 失敗時はnull（XAML側でデフォルトアイコンを表示させるのが楽）
                        return null;
                    }
                });
                //}, _cancellationToken);

                //取得出来たら return して終了
                if(IconImage != null)
                {
                    return IconImage;
                }

                //起動直後のみエラー（Microsoft.WindowsAPICodePack.Shell.ShellException (0x8000000A):）が生じるので、リトライで対処。
                //起動直後に３ファイル以内でのエラー発生傾向なので、いったんこれでも悪影響は無いと判断
                if (IconImage == null)
                {
                    retryCnt++;
                    
                    //Thread.Sleep(100);
                    //await Task.Delay(1000, _cancellationToken);
                    await Task.Delay(1000);

                }
            }
            return IconImage;
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は想定内なので無視して良い
            // アプリ終了時のキャンセルも想定内なので、TaskCanceledException の親クラスの TaskCanceledException を指定
            //_logger.LogTrace("サムネイルの作成はキャンセルされました");
            return null;
        }
        catch (Exception ex)
        {
            //_logger.LogError("LoadThumbnail、Task<BitmapSource?>.Run でキャッチできない例外が発生しました。{ex}", ex);
            return null;
        }

    }

}
