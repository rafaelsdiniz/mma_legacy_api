namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Degraus da trajetória de um lutador. O motor de carreira sobe e desce o
/// atleta por esta escala conforme os resultados, e é ela que determina a
/// qualidade do próximo adversário sorteado.
/// </summary>
public enum EtapaDaCarreira
{
    CircuitoRegional = 1,
    OrganizacaoNacional = 2,
    GrandeOrganizacao = 3,
    Top15 = 4,
    Top5 = 5,

    /// <summary>Luta valendo o cinturão da categoria, sempre em cinco rounds.</summary>
    DisputaDeCinturao = 6,

    /// <summary>Campeão em atividade, defendendo o título em cinco rounds.</summary>
    Campeao = 7
}
