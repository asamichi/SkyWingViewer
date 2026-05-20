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
    }
}