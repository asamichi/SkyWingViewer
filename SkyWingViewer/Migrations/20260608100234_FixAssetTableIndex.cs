using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class FixAssetTableIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_ParentPath",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Asset");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "Asset",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ParentPath",
                table: "Asset",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                table: "Asset",
                column: "Path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_Path",
                table: "Asset");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "Asset",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "ParentPath",
                table: "Asset",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Asset",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ParentPath",
                table: "Asset",
                column: "ParentPath");
        }
    }
}
