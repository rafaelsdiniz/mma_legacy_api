using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data;

/// <summary>
/// Contexto do Entity Framework. Só duas raízes são persistidas diretamente:
/// o acervo de atletas e as partidas.
/// </summary>
/// <remarks>
/// Rodadas do draft, ficha, lutador montado e lutas da carreira não têm
/// <c>DbSet</c> próprio de propósito: eles só existem dentro de uma partida e
/// nunca devem ser consultados ou alterados isoladamente. Deixá-los como tipos
/// pertencidos (owned) faz o EF respeitar essa regra em vez de depender de
/// disciplina de quem escreve a consulta.
/// </remarks>
public sealed class ContextoDoJogo(DbContextOptions<ContextoDoJogo> opcoes) : DbContext(opcoes)
{
    /// <summary>Os atletas reais que aparecem no draft.</summary>
    public DbSet<Lutador> Lutadores => Set<Lutador>();

    /// <summary>As partidas jogadas, com draft, lutador montado e carreira.</summary>
    public DbSet<Partida> Partidas => Set<Partida>();

    /// <summary>As carreiras simuladas, uma por partida.</summary>
    public DbSet<Carreira> Carreiras => Set<Carreira>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        construtor.ApplyConfigurationsFromAssembly(typeof(ContextoDoJogo).Assembly);

        GravarEnumsComoTexto(construtor);
    }

    /// <summary>
    /// Grava todo enum como texto em vez de número.
    /// </summary>
    /// <remarks>
    /// Custa alguns bytes por linha e devolve um banco que se lê sem consultar
    /// o código: "Nocaute" em vez de 1. Num projeto em que as regras ainda vão
    /// mudar bastante, isso também protege contra o pior tipo de bug — inserir
    /// um valor no meio de um enum e reinterpretar silenciosamente todos os
    /// dados já gravados.
    /// </remarks>
    private static void GravarEnumsComoTexto(ModelBuilder construtor)
    {
        var propriedades = construtor.Model
            .GetEntityTypes()
            .SelectMany(entidade => entidade.GetProperties())
            .Where(propriedade => EhEnum(propriedade.ClrType));

        foreach (var propriedade in propriedades)
        {
            propriedade.SetProviderClrType(typeof(string));
        }
    }

    private static bool EhEnum(Type tipo) =>
        (Nullable.GetUnderlyingType(tipo) ?? tipo).IsEnum;
}
