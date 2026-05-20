using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SkyWingViewer.Services;


[Table("AssetTagPair")]
[PrimaryKey(nameof(AssetId), nameof(TagId))]
[Index(nameof(TagId), nameof(AssetId))]
public class AssetTagPairEntity
{
    public int AssetId { get; set; }
    public int TagId { get; set; }

    //ナビゲーションプロパティ

    [ForeignKey(nameof(AssetId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public AssetEntity AssetEntity { get; set; } = null!;
    [ForeignKey(nameof(TagId))]
    //アセットなりタグが削除された時、対応するレコードも一緒に削除される
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public TagsEntity TagsEntity { get; set; } = null!;
}
