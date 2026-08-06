namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Estilo predominante, deduzido dos atributos ao final do draft.
/// Define os bônus e as vantagens de matchup dentro do motor de luta.
/// </summary>
public enum EstiloDeLuta
{
    /// <summary>Striking e potência elevados. Busca a interrupção cedo.</summary>
    Nocauteador = 1,

    /// <summary>Wrestling e cardio elevados. Vence pelo volume de quedas e pressão.</summary>
    WrestlerDePressao = 2,

    /// <summary>Wrestling e jiu-jítsu elevados. Leva a luta para o chão e finaliza.</summary>
    GrapplerCompleto = 3,

    /// <summary>Striking e inteligência de luta elevados. Espera o erro do adversário.</summary>
    ContraGolpeadorTecnico = 4,

    /// <summary>Velocidade e striking elevados. Vence pelo volume e pela movimentação.</summary>
    LutadorDeMovimentacao = 5,

    /// <summary>Todos os atributos equilibrados. Sem vantagens claras, mas sem buracos.</summary>
    LutadorCompleto = 6
}
