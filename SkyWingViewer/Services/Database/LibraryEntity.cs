using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SkyWingViewer.Services;


[Table("Library")]
[PrimaryKey(nameof(Id))]
[Index(nameof(RootPath), IsUnique = true)]
//ユニーク制約をつけるためにインデックスを作成。SQLite ではユニーク制約を付けるためにはインデックスが必要
[Index(nameof(Name), IsUnique = true)]

public class LibraryEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public string RootPath { get; set; } = null!; // ルートの絶対パス (例: "C:\Users\Photo")

    // ナビゲーションプロパティ
    public ICollection<AssetEntity> Assets { get; set; } = new List<AssetEntity>();
}
