namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// O tamanho do estrago: quanto tempo de calendário a lesão come e quantos
/// pontos de atributo ela leva embora para sempre.
/// </summary>
public enum GravidadeDaLesao
{
    /// <summary>Um compromisso parado e nenhuma sequela.</summary>
    Leve = 1,

    /// <summary>Dois compromissos parado e um ponto a menos, permanente.</summary>
    Moderada = 2,

    /// <summary>Três compromissos parado e dois pontos a menos. É a que ameaça a carreira.</summary>
    Grave = 3
}
