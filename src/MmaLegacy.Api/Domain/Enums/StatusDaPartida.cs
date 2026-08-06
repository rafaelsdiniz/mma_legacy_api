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

    /// <summary>A carreira foi simulada e o resultado final está disponível.</summary>
    CarreiraSimulada = 3
}
