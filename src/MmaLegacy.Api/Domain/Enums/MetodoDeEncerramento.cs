namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Como a luta terminou. Alimenta o detalhamento do cartel
/// (vitórias por interrupção, por finalização e por decisão).
/// </summary>
public enum MetodoDeEncerramento
{
    Nocaute = 1,
    Finalizacao = 2,
    Decisao = 3
}
