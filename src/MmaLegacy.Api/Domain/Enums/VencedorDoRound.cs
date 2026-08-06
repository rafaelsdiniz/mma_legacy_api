namespace MmaLegacy.Api.Domain.Enums;

/// <summary>Quem levou um round nos cartões.</summary>
public enum VencedorDoRound
{
    Lutador = 1,
    Adversario = 2,

    /// <summary>Round parelho demais para separar os dois. Ninguém pontua.</summary>
    Nenhum = 3
}
