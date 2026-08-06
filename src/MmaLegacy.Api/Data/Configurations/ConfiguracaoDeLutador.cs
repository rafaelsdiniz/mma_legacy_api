using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data.Configurations;

public sealed class ConfiguracaoDeLutador : IEntityTypeConfiguration<Lutador>
{
    public void Configure(EntityTypeBuilder<Lutador> construtor)
    {
        construtor.ToTable("Lutadores");
        construtor.HasKey(lutador => lutador.Id);

        // O Id vem do slug, calculado no domínio. Sem isto o PostgreSQL tentaria
        // gerar o valor e o seed deixaria de ser idempotente.
        construtor.Property(lutador => lutador.Id).ValueGeneratedNever();

        construtor.Property(lutador => lutador.Nome).HasMaxLength(80).IsRequired();
        // Atletas cadastrados antes deste campo existir entram como em
        // atividade, que é como o jogo os tratava.
        construtor.Property(lutador => lutador.EhLenda)
            .IsRequired()
            .HasDefaultValue(false);

        // Atletas cadastrados antes do elenco do draft existir ficam de fora do
        // sorteio até o seed rodar de novo e reclassificá-los.
        construtor.Property(lutador => lutador.SorteavelNoDraft)
            .IsRequired()
            .HasDefaultValue(false);

        // O draft sorteia só entre os habilitados; sem o índice a consulta varre
        // o acervo inteiro a cada partida criada.
        construtor.HasIndex(lutador => lutador.SorteavelNoDraft);

        construtor.Property(lutador => lutador.Categoria);
        construtor.Property(lutador => lutador.PosicaoNoRanking);

        // A consulta que monta a tabela do ranking ordena por divisão e
        // colocação; sem o índice ela varre o acervo inteiro toda vez.
        construtor.HasIndex(lutador => new { lutador.Categoria, lutador.PosicaoNoRanking });

        construtor.Property(lutador => lutador.Pais).HasMaxLength(60).IsRequired();
        construtor.Property(lutador => lutador.Slug).HasMaxLength(80).IsRequired();

        // O front-end procura a imagem do atleta pelo slug; duas entradas com o
        // mesmo slug apontariam para a mesma foto.
        construtor.HasIndex(lutador => lutador.Slug).IsUnique();

        construtor.OwnsOne(lutador => lutador.Atributos, ConfiguracaoDeAtributos.Aplicar);
        construtor.Navigation(lutador => lutador.Atributos).IsRequired();
    }
}
