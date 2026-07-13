using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asset_Path",
                table: "Asset");

            migrationBuilder.AddColumn<int>(
                name: "LibraryId",
                table: "Asset",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Library",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RootPath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Library", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_LibraryId",
                table: "Asset",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_LibraryId_ParentPath",
                table: "Asset",
                columns: new[] { "LibraryId", "ParentPath" });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path_LibraryId",
                table: "Asset",
                columns: new[] { "Path", "LibraryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Library_Name",
                table: "Library",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Library_RootPath",
                table: "Library",
                column: "RootPath",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Library_LibraryId",
                table: "Asset",
                column: "LibraryId",
                principalTable: "Library",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asset_Library_LibraryId",
                table: "Asset");

            migrationBuilder.DropTable(
                name: "Library");

            migrationBuilder.DropIndex(
                name: "IX_Asset_LibraryId",
                table: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Asset_LibraryId_ParentPath",
                table: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Asset_Path_LibraryId",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "Asset");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Path",
                table: "Asset",
                column: "Path",
                unique: true);
        }
    }
}
