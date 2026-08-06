using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data.Configurations;

/// <summary>
/// Mapeamento dos oito atributos como colunas da própria tabela do dono.
/// </summary>
/// <remarks>
/// <see cref="Atributos"/> aparece em três lugares — no atleta do acervo, no
/// lutador montado e (indiretamente) na carreira. Sem este método compartilhado,
/// a mesma configuração seria copiada três vezes e sairia do ar em uma delas na
/// primeira mudança.
/// </remarks>
internal static class ConfiguracaoDeAtributos
{
    public static void Aplicar<TDono>(OwnedNavigationBuilder<TDono, Atributos> construtor)
        where TDono : class
    {
        construtor.Property(atributos => atributos.Striking).HasColumnName("Striking").IsRequired();
        construtor.Property(atributos => atributos.Potencia).HasColumnName("Potencia").IsRequired();
        construtor.Property(atributos => atributos.Velocidade).HasColumnName("Velocidade").IsRequired();
        construtor.Property(atributos => atributos.Wrestling).HasColumnName("Wrestling").IsRequired();
        construtor.Property(atributos => atributos.JiuJitsu).HasColumnName("JiuJitsu").IsRequired();
        construtor.Property(atributos => atributos.Cardio).HasColumnName("Cardio").IsRequired();
        construtor.Property(atributos => atributos.Resistencia).HasColumnName("Resistencia").IsRequired();
        construtor.Property(atributos => atributos.InteligenciaDeLuta)
            .HasColumnName("InteligenciaDeLuta").IsRequired();
    }
}
