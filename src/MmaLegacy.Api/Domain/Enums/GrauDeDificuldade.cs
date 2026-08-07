namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// O quanto uma oferta é dura para o lutador que a recebe.
/// </summary>
/// <remarks>
/// Não é um atributo do adversário: é a relação entre ele e o jogador de hoje.
/// O mesmo nome que era <see cref="Brutal"/> para um estreante vira
/// <see cref="Tranquila"/> quatro anos depois, e é por isso que o grau nunca é
/// gravado na oferta — ele é derivado toda vez que a mesa é lida.
/// <para>
/// É também o que mede o risco de lesão. Aceitar a luta perigosa deixou de ser
/// só a chance de perder: passou a ser a chance de sair dela sem o mesmo corpo.
/// </para>
/// </remarks>
public enum GrauDeDificuldade
{
    /// <summary>Adversário nitidamente abaixo. Vitória provável, aprendizado quase nenhum.</summary>
    Tranquila = 1,

    /// <summary>Gente do mesmo nível. É onde a maior parte da carreira acontece.</summary>
    Equilibrada = 2,

    /// <summary>Adversário acima do jogador. Vencer acelera a fila; perder cobra caro.</summary>
    Dura = 3,

    /// <summary>Muito acima, ou cinco rounds valendo cinturão. O corpo pode não voltar inteiro.</summary>
    Brutal = 4
}
