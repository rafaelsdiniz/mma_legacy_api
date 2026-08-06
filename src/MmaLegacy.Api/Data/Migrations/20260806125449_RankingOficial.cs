using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RankingOficial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Lutadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosicaoNoRanking",
                table: "Lutadores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lutadores_Categoria_PosicaoNoRanking",
                table: "Lutadores",
                columns: new[] { "Categoria", "PosicaoNoRanking" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lutadores_Categoria_PosicaoNoRanking",
                table: "Lutadores");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Lutadores");

            migrationBuilder.DropColumn(
                name: "PosicaoNoRanking",
                table: "Lutadores");
        }
    }
}
