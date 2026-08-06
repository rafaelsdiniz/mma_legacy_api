namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Porte do evento em que a luta aconteceu. Serve para o histórico e para
/// medir a qualidade do cartel no cálculo de legado: dez vitórias no circuito
/// regional não valem o mesmo que dez em uma grande organização.
/// </summary>
public enum NivelDaOrganizacao
{
    CircuitoRegional = 1,
    OrganizacaoNacional = 2,
    GrandeOrganizacao = 3
}
