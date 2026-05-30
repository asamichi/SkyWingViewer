using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SkyWingViewer.Services;


// = null!; は通常使うべきではないが、EF Core を使う都合上、コンパイラに大丈夫と明示するためここでのみ使用すること。not null 制約なので絶対に大丈夫
//エンティティ
//SQLite では MySQL でいうデータベース、いわゆるスキーマは無いので、指定しない。警告が出る

[Table("Asset")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Name))]
[Index(nameof(FileSize))]
[Index(nameof(Path), IsUnique = true)]
[Index(nameof(ParentPath))]
[Index(nameof(CreationFileTime))]
[Index(nameof(ModifiedTime))]
[Index(nameof(CapturedTime))]
[Index(nameof(AddedTime))]
[Index(nameof(Rating))]
public class AssetEntity
{
    public int Id { get; set; }

    //基礎情報
    [Required] public string Name { get; set; } = null!;
    public long FileSize { get; set; }


    //ファイルなら拡張子。ディレクトリなら Directory
    [Required] public string Type { get; set; } = null!;
    //あるファイルについて、単体で問い合わせる時に必要。
    [Required] public string Path { get; set; } = null!;
    //ターゲットディレクトリにあるかの判定に必要（インデックスを効かせるため正規表現無しで検索したい）
    public string? ParentPath { get; set; } = null;

    //時間系
    [Required] public DateTimeOffset CreationFileTime { get; set; }
    [Required] public DateTimeOffset ModifiedTime { get; set; }
    public DateTimeOffset? CapturedTime { get; set; } = null;
    [Required] public DateTimeOffset AddedTime { get; set; }

    //WILL: DB に対してメモの内容でがっつり検索したい場合や、過去バージョンを残す等したい場合には別テーブルへの切り分けを検討。
    //現状１対１かつシンプルな機能での実装なのでこのままで。検索したいような情報はタグを利用する想定

    public string? Memo { get; set; } = null;
    //画像以外にもこの概念は動画なりにもあるので、このテーブルで良いと判断
    public int? Width { get; set; }
    public int? Height { get; set; }

    public string? ThumbnailPath { get; set; }
    public int Rating { get; set; } = 0;

    //ナビゲーションプロパティ
    public ICollection<AssetTagPairEntity> AssetTagPairs { get; set; } = new List<AssetTagPairEntity>();
}
