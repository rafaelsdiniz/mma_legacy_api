namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// As oito habilidades que compõem um lutador. Cada uma é preenchida
/// exatamente uma vez durante o draft.
/// </summary>
public enum Habilidade
{
    /// <summary>Técnica geral na luta em pé, precisão e variedade de golpes.</summary>
    Striking = 1,

    /// <summary>Capacidade de causar dano e conseguir interrupções.</summary>
    Potencia = 2,

    /// <summary>Movimentação, esquiva e rapidez dos golpes.</summary>
    Velocidade = 3,

    /// <summary>Quedas, defesa de quedas e controle posicional.</summary>
    Wrestling = 4,

    /// <summary>Finalizações, transições e defesa no chão.</summary>
    JiuJitsu = 5,

    /// <summary>Capacidade de manter o desempenho durante os rounds.</summary>
    Cardio = 6,

    /// <summary>Capacidade de suportar golpes, pressão e desgaste.</summary>
    Resistencia = 7,

    /// <summary>Estratégia, adaptação e tomada de decisão durante a luta.</summary>
    InteligenciaDeLuta = 8
}
