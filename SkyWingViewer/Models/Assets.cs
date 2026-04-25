using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

using System.Diagnostics;

namespace SkyWingViewer.Models;


public static class AssetFactory
{
    //対応する拡張子かの判定処理と、自身のインスタンスを作成するメソッド
    //メモ：リストの中身はタプルという機能を使用
    private static readonly List<(Func<string, bool> IsAssetType, Func<string, AssetBase> Create)> _assetCreater = new(){
        (ImageAsset.IsAssetType, ImageAsset.Create)
    };



    public static AssetBase CreateAssetInstance(string path)
    {
        //拡張子を取得
        string extension = Path.GetExtension(path).ToLower();

        //拡張子が対応する AssetType でインスタンスを払い出す
        foreach (var type in _assetCreater)
        {
            if (type.IsAssetType(extension))
            {
                return type.Create(path);
            }
        }
        return OtherAsset.Create(path);

    }
}

//全ファイル共通の基底クラス
public abstract class AssetBase
{
    //アセットの種類一覧
    public enum AssetTypes
    {
        Image,
        Other
    }
    //パス。基底クラスの Path と連動。
    public string AssetPath { get; set; }

    //アセットの種類
    public AssetTypes AssetType { get; set; }

    //拡張子
    public string Extension => Path.GetExtension(AssetPath).ToLower();

    //メタデータ
    public ItemMetadata Metadata { get; set; }


    //TODO: メタデータを追加する
    public AssetBase(string assetPath, AssetTypes assetType)
    {
        AssetPath = assetPath;
        AssetType = assetType;

        //メタデータの取得
        FileInfo fileInfo = new FileInfo(AssetPath);
        Metadata = new ItemMetadata(fileInfo);

    }

}

public interface IAssetBase
{
    //アセットタイプに対応する拡張子かを拡張子から判定
    public static abstract bool IsAssetType(string extension);

    //自身のインスタンスを返す
    static abstract AssetBase Create(string path);
}


//WhenAddType: 対応形式を増やす時は ImageAsset を参考にクラスを追加

//画像系
public class ImageAsset : AssetBase, IAssetBase
{
    public string ThumbnailPath { get; set; }

    //WILL: .clip のような標準で対応していなかったもので独立クラスを必要とするものは、プラグイン形式での追加ができるようにしたい
    //対応する拡張子の一覧
    public static readonly HashSet<string> ImageExtensions = new()
    {
        ".jpg",".jpeg",".png",".bmp",".gif",".webp",".clip"
    };

    //TODO: 画像の詳細情報表示に対応
    //public ImageAssetMetadata ImageMetadata { get; set; }

    public ImageAsset(string assetPath, string thumbnailPath = "") : base(assetPath, AssetTypes.Image)
    {
        ThumbnailPath = thumbnailPath;
    }

    //使わないかも
    public static bool IsImageAsset(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return ImageExtensions.Contains(extension);
    }

    //インタフェース実装
    public static bool IsAssetType(string extension)
    {
        return ImageExtensions.Contains(extension);
    }

    public static AssetBase Create(string path)
    {
        return new ImageAsset(path);
    }

}


//その他の拡張子はすべてこれ
public class OtherAsset : AssetBase, IAssetBase
{

    public OtherAsset(string path) : base(path, AssetTypes.Other)
    {

    }
    //インタフェース実装
    public static bool IsAssetType(string extension)
    {
        return true;
    }

    public static AssetBase Create(string path)
    {
        return new OtherAsset(path);
    }
}