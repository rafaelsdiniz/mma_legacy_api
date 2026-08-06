using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MmaLegacy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lutadores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Pais = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Striking = table.Column<int>(type: "integer", nullable: false),
                    Potencia = table.Column<int>(type: "integer", nullable: false),
                    Velocidade = table.Column<int>(type: "integer", nullable: false),
                    Wrestling = table.Column<int>(type: "integer", nullable: false),
                    JiuJitsu = table.Column<int>(type: "integer", nullable: false),
                    Cardio = table.Column<int>(type: "integer", nullable: false),
                    Resistencia = table.Column<int>(type: "integer", nullable: false),
                    InteligenciaDeLuta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lutadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Apelido = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nacionalidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CategoriaDePeso = table.Column<string>(type: "text", nullable: false),
                    IdadeInicial = table.Column<int>(type: "integer", nullable: false),
                    BaseDeLuta = table.Column<string>(type: "text", nullable: false),
                    Striking = table.Column<int>(type: "integer", nullable: true),
                    Potencia = table.Column<int>(type: "integer", nullable: true),
                    Velocidade = table.Column<int>(type: "integer", nullable: true),
                    Wrestling = table.Column<int>(type: "integer", nullable: true),
                    JiuJitsu = table.Column<int>(type: "integer", nullable: true),
                    Cardio = table.Column<int>(type: "integer", nullable: true),
                    Resistencia = table.Column<int>(type: "integer", nullable: true),
                    InteligenciaDeLuta = table.Column<int>(type: "integer", nullable: true),
                    Overall = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    Estilo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carreiras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdadeDeEstreia = table.Column<int>(type: "integer", nullable: false),
                    IdadeDeAposentadoria = table.Column<int>(type: "integer", nullable: false),
                    Vitorias = table.Column<int>(type: "integer", nullable: false),
                    Derrotas = table.Column<int>(type: "integer", nullable: false),
                    Empates = table.Column<int>(type: "integer", nullable: false),
                    VitoriasPorNocaute = table.Column<int>(type: "integer", nullable: false),
                    VitoriasPorFinalizacao = table.Column<int>(type: "integer", nullable: false),
                    VitoriasPorDecisao = table.Column<int>(type: "integer", nullable: false),
                    CinturoesConquistados = table.Column<int>(type: "integer", nullable: false),
                    DefesasDeCinturao = table.Column<int>(type: "integer", nullable: false),
                    AnosComoCampeao = table.Column<int>(type: "integer", nullable: false),
                    FoiDuploCampeao = table.Column<bool>(type: "boolean", nullable: false),
                    AposentouInvicto = table.Column<bool>(type: "boolean", nullable: false),
                    MaiorSequenciaDeVitorias = table.Column<int>(type: "integer", nullable: false),
                    OverallMaximo = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    CategoriaFinal = table.Column<string>(type: "text", nullable: false),
                    Legado = table.Column<string>(type: "text", nullable: false),
                    PontuacaoDeLegado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carreiras_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RodadasDeDraft",
                columns: table => new
                {
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LutadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LutadorNome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    HabilidadeEscolhida = table.Column<string>(type: "text", nullable: true),
                    NotaObtida = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodadasDeDraft", x => new { x.PartidaId, x.Ordem });
                    table.ForeignKey(
                        name: "FK_RodadasDeDraft_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LutasDaCarreira",
                columns: table => new
                {
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CarreiraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Idade = table.Column<int>(type: "integer", nullable: false),
                    Adversario = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OverallDoAdversario = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    EstiloDoAdversario = table.Column<string>(type: "text", nullable: false),
                    Organizacao = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    DisputaDeCinturao = table.Column<bool>(type: "boolean", nullable: false),
                    DefesaDeCinturao = table.Column<bool>(type: "boolean", nullable: false),
                    RoundsProgramados = table.Column<int>(type: "integer", nullable: false),
                    Resultado = table.Column<string>(type: "text", nullable: false),
                    Metodo = table.Column<string>(type: "text", nullable: false),
                    RoundDoEncerramento = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LutasDaCarreira", x => new { x.CarreiraId, x.Ordem });
                    table.ForeignKey(
                        name: "FK_LutasDaCarreira_Carreiras_CarreiraId",
                        column: x => x.CarreiraId,
                        principalTable: "Carreiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carreiras_PartidaId",
                table: "Carreiras",
                column: "PartidaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lutadores_Slug",
                table: "Lutadores",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lutadores");

            migrationBuilder.DropTable(
                name: "LutasDaCarreira");

            migrationBuilder.DropTable(
                name: "RodadasDeDraft");

            migrationBuilder.DropTable(
                name: "Carreiras");

            migrationBuilder.DropTable(
                name: "Partidas");
        }
    }
}
