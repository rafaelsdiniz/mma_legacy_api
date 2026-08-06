namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Quanta informação o jogador recebe durante o draft.
/// </summary>
/// <remarks>
/// A dificuldade não mexe em nada da simulação — o motor de luta e o de
/// carreira não sabem que ela existe. Ela muda apenas <b>o que a API conta</b>
/// ao jogador enquanto ele decide.
/// <para>
/// Isso é de propósito: dois lutadores idênticos montados em níveis diferentes
/// têm exatamente a mesma carreira. O que muda é o mérito de ter chegado lá.
/// </para>
/// </remarks>
public enum NivelDeDificuldade
{
    /// <summary>
    /// Notas visíveis. O jogador compara números e decide com a informação toda
    /// na mesa.
    /// </summary>
    Facil = 1,

    /// <summary>
    /// Notas ocultas até o fim do draft. O jogador escolhe pelo que sabe de MMA:
    /// pegar wrestling do Khabib é aposta de conhecimento, não de leitura de
    /// tabela.
    /// </summary>
    Dificil = 2
}
