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

//TODO: 実装時に結局他情報からアセット群を取得して、C# 側で絞り込むようになった処理があるため、不要なインデックスについて精査

[Table("Asset")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Name))]
[Index(nameof(FileSize))]
//Path と LibraryId でユニーク制約が必要になるので、下記複合インデックスが必要。
//SQLite では null と null は別のものとして扱われるため、これだけでは Path のユニーク性を確保できないため、Path については DbContext にて部分インデックスを貼る
//[Index(nameof(Path))]
[Index(nameof(Path), nameof(LibraryId), IsUnique = true)]
//[Index(nameof(ParentPath))]
[Index(nameof(LibraryId), nameof(ParentPath))]
[Index(nameof(CreationFileTime))]
[Index(nameof(ModifiedTime))]
[Index(nameof(CapturedTime))]
[Index(nameof(AddedTime))]
[Index(nameof(Rating))]
[Index(nameof(LibraryId))]
public class AssetEntity
{
    public int Id { get; set; }

    //0 ならライブラリに未所属
    public int LibraryId { get; set; } = 0;

    //基礎情報
    [Required] public string Name { get; set; } = null!;
    public long FileSize { get; set; }


    //ファイルなら拡張子。ディレクトリなら Directory
    [Required] public string Type { get; set; } = null!;
    //あるファイルについて、単体で問い合わせる時に必要。
    [Required] public string Path { get; set; } = null!;
    //ターゲットディレクトリにあるかの判定に必要（インデックスを効かせるため正規表現無しで検索したい）
    //Path のみだと、さらに子のフォルダまで引っかかるので、前方一致のみでは判別できない
    [Required] public string ParentPath { get; set; } = null!;

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

    //public string? ThumbnailPath { get; set; }
    public int Rating { get; set; } = 0;


    //ナビゲーションプロパティ
    public ICollection<AssetTagPairEntity> AssetTagPairs { get; set; } = new List<AssetTagPairEntity>();

    //LibraryId は存在するライブラリ ID のみを許容（外部キー制約）。
    [ForeignKey(nameof(LibraryId))]
    // ライブラリ登録を削除した際には、null にする。どのみち手動で本来のパスの方の変換処理を実施するので、念のための設定
    [DeleteBehavior(DeleteBehavior.SetNull)] 
    public LibraryEntity? Library { get; set; }


}
