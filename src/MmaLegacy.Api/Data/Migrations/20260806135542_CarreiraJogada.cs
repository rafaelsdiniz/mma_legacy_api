using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CarreiraJogada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AjusteDeOverallDoAdversario",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cardio",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategoriaAtual",
                table: "Carreiras",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompromissosNaTemporada",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefesasNaCategoria",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DerrotasSeguidas",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EhCampeao",
                table: "Carreiras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Encerrada",
                table: "Carreiras",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Etapa",
                table: "Carreiras",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdadeAtual",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InteligenciaDeLuta",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "JaMudouDeCategoria",
                table: "Carreiras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "JiuJitsu",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDoEncerramento",
                table: "Carreiras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NocautesSofridos",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NocautesSofridosNoAno",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OverallMaximoDoEstado",
                table: "Carreiras",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Passo",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Potencia",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecusasSeguidas",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Resistencia",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SequenciaAtualDeVitorias",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Striking",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Velocidade",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VezesDispensado",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VitoriasNaEtapa",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wrestling",
                table: "Carreiras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OfertasDeLuta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Indice = table.Column<int>(type: "integer", nullable: false),
                    Adversario = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CartelDoAdversario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Striking = table.Column<int>(type: "integer", nullable: false),
                    Potencia = table.Column<int>(type: "integer", nullable: false),
                    Velocidade = table.Column<int>(type: "integer", nullable: false),
                    Wrestling = table.Column<int>(type: "integer", nullable: false),
                    JiuJitsu = table.Column<int>(type: "integer", nullable: false),
                    Cardio = table.Column<int>(type: "integer", nullable: false),
                    Resistencia = table.Column<int>(type: "integer", nullable: false),
                    InteligenciaDeLuta = table.Column<int>(type: "integer", nullable: false),
                    Organizacao = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    DisputaDeCinturao = table.Column<bool>(type: "boolean", nullable: false),
                    DefesaDeCinturao = table.Column<bool>(type: "boolean", nullable: false),
                    RoundsProgramados = table.Column<int>(type: "integer", nullable: false),
                    Chamada = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CarreiraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertasDeLuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfertasDeLuta_Carreiras_CarreiraId",
                        column: x => x.CarreiraId,
                        principalTable: "Carreiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfertasDeLuta_CarreiraId",
                table: "OfertasDeLuta",
                column: "CarreiraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfertasDeLuta");

            migrationBuilder.DropColumn(
                name: "AjusteDeOverallDoAdversario",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Cardio",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "CategoriaAtual",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "CompromissosNaTemporada",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "DefesasNaCategoria",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "DerrotasSeguidas",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "EhCampeao",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Encerrada",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Etapa",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "IdadeAtual",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "InteligenciaDeLuta",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "JaMudouDeCategoria",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "JiuJitsu",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "MotivoDoEncerramento",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "NocautesSofridos",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "NocautesSofridosNoAno",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "OverallMaximoDoEstado",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Passo",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Potencia",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "RecusasSeguidas",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Resistencia",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "SequenciaAtualDeVitorias",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Striking",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Velocidade",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "VezesDispensado",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "VitoriasNaEtapa",
                table: "Carreiras");

            migrationBuilder.DropColumn(
                name: "Wrestling",
                table: "Carreiras");
        }
    }
}
