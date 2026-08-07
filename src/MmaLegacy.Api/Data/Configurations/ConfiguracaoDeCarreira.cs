using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data.Configurations;

public sealed class ConfiguracaoDeCarreira : IEntityTypeConfiguration<Carreira>
{
    public void Configure(EntityTypeBuilder<Carreira> construtor)
    {
        construtor.ToTable("Carreiras");
        construtor.HasKey(carreira => carreira.Id);
        construtor.Property(carreira => carreira.Id).ValueGeneratedNever();

        construtor.HasIndex(carreira => carreira.PartidaId).IsUnique();

        construtor.Property(carreira => carreira.OverallMaximo).HasPrecision(4, 1);

        // Carreiras gravadas antes de o jogo ser jogável luta a luta já vieram
        // ao mundo fechadas: elas eram o resultado de uma simulação inteira.
        construtor.Property(carreira => carreira.Encerrada)
            .IsRequired()
            .HasDefaultValue(true);

        // Tudo isto sai da lista de lutas em Carreira. Persistir os números é o
        // que permite montar a tela de resultado e o ranking sem recarregar
        // dezenas de lutas por partida.
        construtor.Ignore(carreira => carreira.FoiCampeao);
        construtor.Ignore(carreira => carreira.TotalDeLutas);
        construtor.Ignore(carreira => carreira.Cartel);

        ConfigurarEstado(construtor);
        ConfigurarLutas(construtor);
        ConfigurarOfertas(construtor);
        ConfigurarRivais(construtor);
    }

    /// <summary>
    /// O estado vira colunas da própria tabela de carreiras: é um único registro
    /// por carreira e nunca é consultado sozinho.
    /// </summary>
    private static void ConfigurarEstado(EntityTypeBuilder<Carreira> construtor)
    {
        construtor.OwnsOne(carreira => carreira.Estado, estado =>
        {
            estado.Property(dado => dado.Idade).HasColumnName("IdadeAtual");
            estado.Property(dado => dado.Categoria).HasColumnName("CategoriaAtual");
            estado.Property(dado => dado.Etapa).HasColumnName("Etapa");
            estado.Property(dado => dado.OverallMaximo)
                .HasColumnName("OverallMaximoDoEstado").HasPrecision(4, 1);
            estado.Property(dado => dado.VitoriasNaEtapa).HasColumnName("VitoriasNaEtapa");
            estado.Property(dado => dado.DerrotasSeguidas).HasColumnName("DerrotasSeguidas");
            estado.Property(dado => dado.RecusasSeguidas).HasColumnName("RecusasSeguidas");
            estado.Property(dado => dado.NocautesSofridos).HasColumnName("NocautesSofridos");
            estado.Property(dado => dado.NocautesSofridosNoAno).HasColumnName("NocautesSofridosNoAno");
            estado.Property(dado => dado.DefesasNaCategoria).HasColumnName("DefesasNaCategoria");
            estado.Property(dado => dado.EhCampeao).HasColumnName("EhCampeao");
            estado.Property(dado => dado.JaMudouDeCategoria).HasColumnName("JaMudouDeCategoria");
            estado.Property(dado => dado.AjusteDeOverallDoAdversario)
                .HasColumnName("AjusteDeOverallDoAdversario");
            estado.Property(dado => dado.CompromissosNaTemporada).HasColumnName("CompromissosNaTemporada");
            estado.Property(dado => dado.VezesDispensado).HasColumnName("VezesDispensado");
            estado.Property(dado => dado.LesoesSofridas).HasColumnName("LesoesSofridas");
            estado.Property(dado => dado.Passo).HasColumnName("Passo");
            estado.Property(dado => dado.PosicaoNoRanking).HasColumnName("PosicaoNoRanking");

            estado.Ignore(dado => dado.OverallAtual);
            estado.Ignore(dado => dado.Estilo);
            estado.Ignore(dado => dado.EstaRanqueado);
            estado.Ignore(dado => dado.EstaLesionado);

            ConfigurarLesao(estado);

            estado.OwnsOne(dado => dado.Atributos, ConfiguracaoDeAtributos.Aplicar);
            estado.Navigation(dado => dado.Atributos).IsRequired();
        });

        construtor.Navigation(carreira => carreira.Estado).IsRequired();
    }

    /// <summary>
    /// A lesão em tratamento vira colunas anuláveis da própria carreira. É uma
    /// só por vez e nunca é consultada sozinha — uma tabela própria seria uma
    /// junção a mais para guardar cinco números que só existem enquanto o
    /// lutador está machucado.
    /// </summary>
    private static void ConfigurarLesao(OwnedNavigationBuilder<Carreira, EstadoDaCarreira> estado)
    {
        estado.OwnsOne(dado => dado.LesaoAtual, lesao =>
        {
            lesao.Property(dado => dado.Tipo).HasColumnName("TipoDaLesao");
            lesao.Property(dado => dado.Gravidade).HasColumnName("GravidadeDaLesao");
            lesao.Property(dado => dado.Afastamento).HasColumnName("AfastamentoDaLesao");
            lesao.Property(dado => dado.CompromissosRestantes)
                .HasColumnName("CompromissosDeRecuperacao");
            lesao.Property(dado => dado.IdadeQuandoOcorreu).HasColumnName("IdadeQuandoSeLesionou");

            lesao.Ignore(dado => dado.Sarou);
        });
    }

    private static void ConfigurarLutas(EntityTypeBuilder<Carreira> construtor)
    {
        construtor.OwnsMany(carreira => carreira.Lutas, luta =>
        {
            luta.ToTable("LutasDaCarreira");
            luta.WithOwner().HasForeignKey("CarreiraId");
            luta.HasKey("CarreiraId", nameof(LutaDaCarreira.Ordem));

            luta.Property(dado => dado.Adversario).HasMaxLength(80).IsRequired();
            luta.Property(dado => dado.OverallDoAdversario).HasPrecision(4, 1);

            luta.Ignore(dado => dado.ValendoCinturao);
        });

        construtor.Navigation(carreira => carreira.Lutas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <summary>
    /// As ofertas na mesa. Tabela própria porque são várias por carreira, e
    /// descartáveis: a rodada seguinte apaga a anterior.
    /// </summary>
    private static void ConfigurarOfertas(EntityTypeBuilder<Carreira> construtor)
    {
        construtor.OwnsMany(carreira => carreira.Ofertas, oferta =>
        {
            oferta.ToTable("OfertasDeLuta");
            oferta.WithOwner().HasForeignKey("CarreiraId");
            oferta.HasKey(dado => dado.Id);
            oferta.Property(dado => dado.Id).ValueGeneratedNever();

            oferta.Property(dado => dado.Adversario).HasMaxLength(80).IsRequired();
            oferta.Property(dado => dado.CartelDoAdversario).HasMaxLength(20).IsRequired();
            oferta.Property(dado => dado.Chamada).HasMaxLength(120).IsRequired();

            // Preenchidos só quando o adversário é um atleta real do acervo. No
            // circuito regional e na LFA ele é inventado, e não tem foto nem
            // lugar no ranking.
            oferta.Property(dado => dado.SlugDoAdversario).HasMaxLength(80);
            oferta.Property(dado => dado.PosicaoDoAdversario);

            // Overall e estilo são derivados dos atributos na leitura, pelas
            // mesmas regras que classificam o lutador do jogador. Gravá-los
            // criaria a chance de uma oferta salva hoje divergir da
            // classificação de amanhã.
            oferta.Ignore(dado => dado.OverallDoAdversario);
            oferta.Ignore(dado => dado.EstiloDoAdversario);
            oferta.Ignore(dado => dado.ValendoCinturao);
            oferta.Ignore(dado => dado.EhRevanche);

            oferta.OwnsOne(dado => dado.AtributosDoAdversario, ConfiguracaoDeAtributos.Aplicar);
            oferta.Navigation(dado => dado.AtributosDoAdversario).IsRequired();
        });

        construtor.Navigation(carreira => carreira.Ofertas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <summary>
    /// Os adversários de fora do ranking que a carreira guarda para uma
    /// revanche. Tabela própria porque são vários e sobrevivem a várias
    /// rodadas de oferta — ao contrário das ofertas, que a rodada seguinte
    /// apaga.
    /// </summary>
    private static void ConfigurarRivais(EntityTypeBuilder<Carreira> construtor)
    {
        construtor.OwnsMany(carreira => carreira.Rivais, rival =>
        {
            rival.ToTable("RivaisDaCarreira");
            rival.WithOwner().HasForeignKey("CarreiraId");
            rival.HasKey(dado => dado.Id);
            rival.Property(dado => dado.Id).ValueGeneratedNever();

            rival.Property(dado => dado.Nome).HasMaxLength(80).IsRequired();
            rival.Property(dado => dado.Cartel).HasMaxLength(20).IsRequired();

            rival.Ignore(dado => dado.TemContaAAcertar);
            rival.Ignore(dado => dado.TotalDeEncontros);

            rival.OwnsOne(dado => dado.Atributos, ConfiguracaoDeAtributos.Aplicar);
            rival.Navigation(dado => dado.Atributos).IsRequired();
        });

        construtor.Navigation(carreira => carreira.Rivais)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
