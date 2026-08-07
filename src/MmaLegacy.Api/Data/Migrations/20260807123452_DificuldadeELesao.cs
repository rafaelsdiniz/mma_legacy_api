using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DificuldadeELesao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AfastamentoDaLesao",
                table: "Carreiras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompromissosDeRecuperacao",
                table: "Carreiras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GravidadeDaLesao",
                table: "Carreiras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdadeQuandoSeLesionou",
                table: "Carreiras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LesoesSofridas",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TipoDaLesao",
                table: "Carreiras",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfastamentoDaLesao",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "CompromissosDeRecuperacao",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "GravidadeDaLesao",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "IdadeQuandoSeLesionou",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "LesoesSofridas",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "TipoDaLesao",
                table: "Carreiras");
        }
    }
}
