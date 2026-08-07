using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RivaisDaCarreira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DerrotasDoAdversarioParaVoce",
                table: "OfertasDeLuta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RivalId",
                table: "OfertasDeLuta",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitoriasDoAdversarioSobreVoce",
                table: "OfertasDeLuta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RivaisDaCarreira",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cartel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Striking = table.Column<int>(type: "integer", nullable: false),
                    Potencia = table.Column<int>(type: "integer", nullable: false),
                    Velocidade = table.Column<int>(type: "integer", nullable: false),
                    Wrestling = table.Column<int>(type: "integer", nullable: false),
                    JiuJitsu = table.Column<int>(type: "integer", nullable: false),
                    Cardio = table.Column<int>(type: "integer", nullable: false),
                    Resistencia = table.Column<int>(type: "integer", nullable: false),
                    InteligenciaDeLuta = table.Column<int>(type: "integer", nullable: false),
                    VitoriasSobreOJogador = table.Column<int>(type: "integer", nullable: false),
                    DerrotasParaOJogador = table.Column<int>(type: "integer", nullable: false),
                    EmpatesComOJogador = table.Column<int>(type: "integer", nullable: false),
                    OrdemDoUltimoEncontro = table.Column<int>(type: "integer", nullable: false),
                    ResultadoDoUltimoEncontro = table.Column<string>(type: "text", nullable: false),
                    MetodoDoUltimoEncontro = table.Column<string>(type: "text", nullable: false),
                    CarreiraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RivaisDaCarreira", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RivaisDaCarreira_Carreiras_CarreiraId",
                        column: x => x.CarreiraId,
                        principalTable: "Carreiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RivaisDaCarreira_CarreiraId",
                table: "RivaisDaCarreira",
                column: "CarreiraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RivaisDaCarreira");

            migrationBuilder.DropColumn(
                name: "DerrotasDoAdversarioParaVoce",
                table: "OfertasDeLuta");

            migrationBuilder.DropColumn(
                name: "RivalId",
                table: "OfertasDeLuta");

            migrationBuilder.DropColumn(
                name: "VitoriasDoAdversarioSobreVoce",
                table: "OfertasDeLuta");
        }
    }
}
