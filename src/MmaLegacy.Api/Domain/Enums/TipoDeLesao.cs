namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// O que o lutador machucou. Cada tipo cobra de uma habilidade diferente, o que
/// faz duas carreiras com o mesmo número de lesões terminarem diferentes.
/// </summary>
public enum TipoDeLesao
{
    /// <summary>Corte profundo. Custa tempo de calendário e nada mais.</summary>
    Corte = 1,

    /// <summary>Mão fraturada — a moeda de quem soca forte. Cobra potência.</summary>
    MaoFraturada = 2,

    /// <summary>Joelho lesionado. Cobra velocidade, e velocidade não volta.</summary>
    JoelhoLesionado = 3,

    /// <summary>Costela trincada. Cobra cardio: respirar volta a doer.</summary>
    CostelaTrincada = 4,

    /// <summary>Concussão. Cobra resistência e é a que encurta carreiras.</summary>
    Concussao = 5
}
