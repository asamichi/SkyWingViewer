using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SkyWingViewer.Services;


[Table("Tags")]
[PrimaryKey(nameof(Id))]
[Index(nameof(TagName), IsUnique = true)]
public class TagsEntity
{
    public int Id { get; set; }
    [Required] public string TagName { get; set; } = null!;

    //ナビゲーションプロパティ
    public ICollection<AssetTagPairEntity> AssetTagPairs { get; set; } = new List<AssetTagPairEntity>();
}