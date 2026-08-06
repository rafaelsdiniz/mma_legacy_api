using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data.Configurations;

public sealed class ConfiguracaoDePartida : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> construtor)
    {
        construtor.ToTable("Partidas");
        construtor.HasKey(partida => partida.Id);
        construtor.Property(partida => partida.Id).ValueGeneratedNever();

        construtor.Property(partida => partida.Seed).IsRequired();
        construtor.Property(partida => partida.CriadaEm).IsRequired();
        construtor.Property(partida => partida.Status).IsRequired();

        // Derivados dos demais campos: recalcular é mais barato que manter
        // sincronizado, e uma coluna a menos é uma inconsistência a menos.
        construtor.Ignore(partida => partida.EscolhasFeitas);
        construtor.Ignore(partida => partida.SeedDaCarreira);

        ConfigurarFicha(construtor);
        ConfigurarLutadorMontado(construtor);
        ConfigurarRodadas(construtor);
        ConfigurarCarreira(construtor);
    }

    private static void ConfigurarFicha(EntityTypeBuilder<Partida> construtor)
    {
        construtor.OwnsOne(partida => partida.Ficha, ficha =>
        {
            ficha.Property(dado => dado.Nome)
                .HasColumnName("Nome").HasMaxLength(FichaDeInscricao.TamanhoMaximoDoNome).IsRequired();
            ficha.Property(dado => dado.Apelido)
                .HasColumnName("Apelido").HasMaxLength(FichaDeInscricao.TamanhoMaximoDoApelido).IsRequired();
            ficha.Property(dado => dado.Nacionalidade)
                .HasColumnName("Nacionalidade").HasMaxLength(60).IsRequired();
            ficha.Property(dado => dado.CategoriaDePeso).HasColumnName("CategoriaDePeso").IsRequired();
            ficha.Property(dado => dado.IdadeInicial).HasColumnName("IdadeInicial").IsRequired();
            ficha.Property(dado => dado.BaseDeLuta).HasColumnName("BaseDeLuta").IsRequired();
        });

        construtor.Navigation(partida => partida.Ficha).IsRequired();
    }

    private static void ConfigurarLutadorMontado(EntityTypeBuilder<Partida> construtor)
    {
        construtor.OwnsOne(partida => partida.Lutador, lutador =>
        {
            lutador.Property(dado => dado.Overall).HasColumnName("Overall").HasPrecision(4, 1);
            lutador.Property(dado => dado.Estilo).HasColumnName("Estilo");

            lutador.Ignore(dado => dado.MaiorQualidade);
            lutador.Ignore(dado => dado.PrincipalFraqueza);

            lutador.OwnsOne(dado => dado.Atributos, ConfiguracaoDeAtributos.Aplicar);
            lutador.Navigation(dado => dado.Atributos).IsRequired();
        });
    }

    private static void ConfigurarRodadas(EntityTypeBuilder<Partida> construtor)
    {
        construtor.OwnsMany(partida => partida.Rodadas, rodada =>
        {
            rodada.ToTable("RodadasDeDraft");
            rodada.WithOwner().HasForeignKey("PartidaId");

            // A rodada não tem identidade própria: ela é a n-ésima rodada de
            // uma partida, e essa dupla já a identifica sem inventar um Id.
            rodada.HasKey("PartidaId", nameof(RodadaDeDraft.Ordem));

            rodada.Property(dado => dado.Ordem).IsRequired();
            rodada.Property(dado => dado.LutadorId).IsRequired();
            rodada.Property(dado => dado.LutadorNome).HasMaxLength(80).IsRequired();
            rodada.Property(dado => dado.HabilidadeEscolhida);
            rodada.Property(dado => dado.NotaObtida);

            rodada.Ignore(dado => dado.Concluida);
        });

        // A coleção é somente-leitura para o mundo externo; quem escreve nela é
        // o campo privado, e é dele que o EF precisa.
        construtor.Navigation(partida => partida.Rodadas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigurarCarreira(EntityTypeBuilder<Partida> construtor)
    {
        // A carreira é entidade separada, não tipo pertencido: ela tem dezenas
        // de lutas e é consultada sozinha na tela de resultado.
        construtor.HasOne(partida => partida.Carreira)
            .WithOne()
            .HasForeignKey<Carreira>(carreira => carreira.PartidaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
