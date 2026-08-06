namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Etapas pelas quais uma partida passa. A transição é sempre em ordem
/// crescente e cada operação da API só é válida em um status específico.
/// </summary>
public enum StatusDaPartida
{
    /// <summary>O draft foi sorteado e ainda há habilidades a preencher.</summary>
    DraftEmAndamento = 1,

    /// <summary>As oito habilidades foram preenchidas e o lutador foi montado.</summary>
    DraftConcluido = 2,

    /// <summary>A carreira acabou e o resultado final está disponível.</summary>
    CarreiraSimulada = 3,

    /// <summary>
    /// O lutador estreou e ainda está em atividade: há ofertas de luta na mesa
    /// esperando uma decisão do jogador.
    /// </summary>
    /// <remarks>
    /// Numerado depois de <see cref="CarreiraSimulada"/> apesar de vir antes
    /// dela na vida de uma partida. Os enums são gravados como texto, então a
    /// ordem numérica não significa nada para o banco — e renumerar o que já
    /// está gravado significaria reinterpretar linhas antigas.
    /// </remarks>
    CarreiraEmAndamento = 4
}
