using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RankingDaCarreira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PosicaoDoAdversario",
                table: "OfertasDeLuta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugDoAdversario",
                table: "OfertasDeLuta",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosicaoNoRanking",
                table: "Carreiras",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosicaoDoAdversario",
                table: "OfertasDeLuta");

            migrationBuilder.DropColumn(
                name: "SlugDoAdversario",
                table: "OfertasDeLuta");

            migrationBuilder.DropColumn(
                name: "PosicaoNoRanking",
                table: "Carreiras");
        }
    }
}
