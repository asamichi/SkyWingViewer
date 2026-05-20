using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyWingViewer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaForSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Tags",
                schema: "SkyWingViewer",
                newName: "Tags");

            migrationBuilder.RenameTable(
                name: "AssetTagPair",
                schema: "SkyWingViewer",
                newName: "AssetTagPair");

            migrationBuilder.RenameTable(
                name: "Asset",
                schema: "SkyWingViewer",
                newName: "Asset");

            migrationBuilder.AlterColumn<string>(
                name: "ParentPath",
                table: "Asset",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SkyWingViewer");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "Tags",
                newSchema: "SkyWingViewer");

            migrationBuilder.RenameTable(
                name: "AssetTagPair",
                newName: "AssetTagPair",
                newSchema: "SkyWingViewer");

            migrationBuilder.RenameTable(
                name: "Asset",
                newName: "Asset",
                newSchema: "SkyWingViewer");

            migrationBuilder.AlterColumn<string>(
                name: "ParentPath",
                schema: "SkyWingViewer",
                table: "Asset",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
