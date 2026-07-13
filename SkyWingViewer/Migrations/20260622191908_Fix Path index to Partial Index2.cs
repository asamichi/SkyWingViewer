using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class FixPathindextoPartialIndex2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_Path",
                table: "Asset");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                table: "Asset",
                column: "Path",
                unique: true,
                filter: "[LibraryId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_Path",
                table: "Asset");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                table: "Asset",
                column: "Path",
                unique: true);
        }
    }
}
