using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SkyWingViewer");

            migrationBuilder.CreateTable(
                name: "Asset",
                schema: "SkyWingViewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    ParentPath = table.Column<string>(type: "TEXT", nullable: false),
                    CreationFileTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CapturedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AddedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Memo = table.Column<string>(type: "TEXT", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "SkyWingViewer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetTagPair",
                schema: "SkyWingViewer",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTagPair", x => new { x.AssetId, x.TagId });
                    table.ForeignKey(
                        name: "FK_AssetTagPair_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "SkyWingViewer",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetTagPair_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "SkyWingViewer",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_AddedTime",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "AddedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_CapturedTime",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "CapturedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_CreationFileTime",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "CreationFileTime");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_FileSize",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "FileSize");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ModifiedTime",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "ModifiedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Name",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ParentPath",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "ParentPath");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Rating",
                schema: "SkyWingViewer",
                table: "Asset",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTagPair_TagId_AssetId",
                schema: "SkyWingViewer",
                table: "AssetTagPair",
                columns: new[] { "TagId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagName",
                schema: "SkyWingViewer",
                table: "Tags",
                column: "TagName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetTagPair",
                schema: "SkyWingViewer");

            migrationBuilder.DropTable(
                name: "Asset",
                schema: "SkyWingViewer");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "SkyWingViewer");
        }
    }
}
