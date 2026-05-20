namespace DatabaseTest;

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SkyWingViewer.Models;
using SkyWingViewer.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


public class AssetEntityTests
{
    [Fact]
    public void TestAssetEntityConstructor()
    {
        var asset = new AssetEntity
        {
            Id = 1,
            Name = "test.jpg",
            FileSize = 1024L,
            Path = "C:\\path\\to\\file.jpg",
            CreationFileTime = DateTimeOffset.Now,
            ModifiedTime = DateTimeOffset.Now,
            ParentPath = "C:\\path\\to"
        };

        Assert.Equal(1, asset.Id);
        Assert.Equal("test.jpg", asset.Name);
        Assert.Equal(1024L, asset.FileSize);
        Assert.Equal("C:\\path\\to\\file.jpg", asset.Path);
        Assert.NotEqual(DateTimeOffset.MinValue, asset.CreationFileTime);
        Assert.NotEqual(DateTimeOffset.MinValue, asset.ModifiedTime);
        Assert.Equal("C:\\path\\to", asset.ParentPath);
    }
}

public class DatabaseServiceTests
{
    private readonly MyDbContext _dbContext;
    private readonly DatabaseService _databaseService;

    public DatabaseServiceTests()
    {
        // SQLite データベースを使用する場合
        var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
        optionsBuilder.UseSqlite("Data Source=TestDB.db");

        _dbContext = new MyDbContext(optionsBuilder.Options);
        _dbContext.Database.EnsureCreated();

        _databaseService = new DatabaseService(_dbContext);
    }

    [Fact]
    public async Task TestCreateAssetEntity()
    {
        var fileSystemItemBase = new ImageAsset("C:\\path\\to\\file.jpg");
        var asset = _databaseService.CreateAssetEntity(fileSystemItemBase);

        Assert.NotNull(asset);
        Assert.Equal("file.jpg", asset.Name);
        // その他のアサーションを追加します。
    }

    [Fact]
    public async Task TestCreateTagEntity()
    {
        var tag = _databaseService.CreateTagEntity("testTag");

        Assert.NotNull(tag);
        Assert.Equal("testTag", tag.TagName);
    }
}