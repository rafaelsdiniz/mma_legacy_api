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

        // Tudo isto sai da lista de lutas em Carreira.Encerrar. Persistir os
        // números é o que permite montar a tela de resultado e o ranking sem
        // recarregar dezenas de lutas por partida.
        construtor.Ignore(carreira => carreira.FoiCampeao);
        construtor.Ignore(carreira => carreira.TotalDeLutas);
        construtor.Ignore(carreira => carreira.Cartel);

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
}
