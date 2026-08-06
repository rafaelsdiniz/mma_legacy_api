using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NivelDeDificuldade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NivelDeDificuldade",
                table: "Partidas",
                type: "text",
                nullable: false,
                defaultValue: "Facil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NivelDeDificuldade",
                table: "Partidas");
        }
    }
}
