using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWingViewer.Services;



public class MyDbContext : DbContext
{
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<TagsEntity> Tags => Set<TagsEntity>();
    public DbSet<AssetTagPairEntity> AssetTagPairs => Set<AssetTagPairEntity>();

    public DbSet<LibraryEntity> Library => Set<LibraryEntity>();

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    // データベース接続文字列（ファイル名）
    //    optionsBuilder.UseSqlite("Data Source=SkyWingViewer.db");
    //}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // スキーマの設定は SQLite にはない
        //modelBuilder.HasDefaultSchema("SkyWingViewer");

        //Path 関連の列について、大文字小文字区別なしにする
        //Windows のパスでは大文字小文字は区別されない
        modelBuilder.Entity<AssetEntity>().Property(a => a.Path).UseCollation("NOCASE");
        modelBuilder.Entity<AssetEntity>().Property(a => a.ParentPath).UseCollation("NOCASE");

        //部分インデックス
        modelBuilder.Entity<AssetEntity>().HasIndex(a => a.Path).HasFilter("[LibraryId] IS NULL").IsUnique();

    }
}