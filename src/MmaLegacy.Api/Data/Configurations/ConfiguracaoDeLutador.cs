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
        construtor.Property(lutador => lutador.Pais).HasMaxLength(60).IsRequired();
        construtor.Property(lutador => lutador.Slug).HasMaxLength(80).IsRequired();

        // O front-end procura a imagem do atleta pelo slug; duas entradas com o
        // mesmo slug apontariam para a mesma foto.
        construtor.HasIndex(lutador => lutador.Slug).IsUnique();

        construtor.OwnsOne(lutador => lutador.Atributos, ConfiguracaoDeAtributos.Aplicar);
        construtor.Navigation(lutador => lutador.Atributos).IsRequired();
    }
}
