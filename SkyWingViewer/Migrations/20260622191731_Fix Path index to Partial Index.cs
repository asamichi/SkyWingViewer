using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class FixPathindextoPartialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
