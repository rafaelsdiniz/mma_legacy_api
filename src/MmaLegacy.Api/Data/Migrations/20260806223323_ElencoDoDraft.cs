using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ElencoDoDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SorteavelNoDraft",
                table: "Lutadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Lutadores_SorteavelNoDraft",
                table: "Lutadores",
                column: "SorteavelNoDraft");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lutadores_SorteavelNoDraft",
                table: "Lutadores");

            migrationBuilder.DropColumn(
                name: "SorteavelNoDraft",
                table: "Lutadores");
        }
    }
}
