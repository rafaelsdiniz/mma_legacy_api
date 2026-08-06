using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PulosNoDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PulosUsados",
                table: "Partidas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PulosUsados",
                table: "Partidas");
        }
    }
}
