using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;


namespace SkyWingViewer.Services;

//class DirectReadBitmapImage : IThumbnailProvider
class DirectReadBitmapImage
{
    //下記に記載の拡張子には対応している処理 追記：OS 側の設定などで実は対応している形式がある、webp 等。環境依存っぽい。方法としてはこれが早そうではあるので、とりあえずこれは試すようにする。
    //https://learn.microsoft.com/ja-jp/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-26100
    HashSet<string> SupportedExtensions { get; } = new()
    {
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".tiff",
        ".ico",
        ".jxr",
        ".wdp",
        ".hdp"
    };

    //CreateThumbnailFile から移植
    public static BitmapImage? GetBitmapImage(string filePath, int ThumbnailSizeWidth,int ThumbnailSizeHeight)
    {
        BitmapImage original = new();

        //下記に記載の拡張子には対応している処理 追記：OS 側の設定などで実は対応している形式がある、webp 等。環境依存っぽい。方法としてはこれが早そうではあるので、とりあえずこれは試すようにする。
        //https://learn.microsoft.com/ja-jp/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-26100

        try
        {
            //読み取り専用の SLock、バッファサイズを 64KB に、シーケンシャルアクセス明示
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            {
                original.BeginInit();
                original.CacheOption = BitmapCacheOption.OnLoad;
                original.StreamSource = stream;
                //サイズ自体は暫定。簡易的な最適化として
                original.DecodePixelWidth = ThumbnailSizeWidth * 2;
                original.EndInit();
                original.Freeze();
            }

            return original;
        }
        catch
        {
            //読み取れない形式の場合 null を返す
            return null;
        }

    }
}



