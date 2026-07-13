using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class removeUniquePathindexinAssetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_Path",
                table: "Asset");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                table: "Asset",
                column: "Path",
                unique: true);
        }
    }
}
