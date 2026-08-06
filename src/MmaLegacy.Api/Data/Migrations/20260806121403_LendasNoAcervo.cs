using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LendasNoAcervo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EhLenda",
                table: "Lutadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EhLenda",
                table: "Lutadores");
        }
    }
}
