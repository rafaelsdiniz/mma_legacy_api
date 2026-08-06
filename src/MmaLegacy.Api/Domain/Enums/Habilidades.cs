namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Utilitários sobre o enum <see cref="Habilidade"/>.
/// </summary>
/// <remarks>
/// Centralizar aqui a lista completa evita que cada regra do jogo repita os
/// oito valores — e garante que adicionar uma nona habilidade um dia seja uma
/// mudança em um lugar só.
/// </remarks>
public static class Habilidades
{
    /// <summary>Todas as habilidades, na ordem em que são exibidas ao jogador.</summary>
    public static readonly IReadOnlyList<Habilidade> Todas = Enum.GetValues<Habilidade>();

    /// <summary>Quantas escolhas o draft exige para montar um lutador.</summary>
    public static int Quantidade => Todas.Count;

    private static readonly Dictionary<Habilidade, string> NomesDeExibicao = new()
    {
        [Habilidade.Striking] = "Striking",
        [Habilidade.Potencia] = "Potência",
        [Habilidade.Velocidade] = "Velocidade",
        [Habilidade.Wrestling] = "Wrestling",
        [Habilidade.JiuJitsu] = "Jiu-jítsu",
        [Habilidade.Cardio] = "Cardio",
        [Habilidade.Resistencia] = "Resistência",
        [Habilidade.InteligenciaDeLuta] = "Fight IQ"
    };

    /// <summary>
    /// Nome acentuado da habilidade, como aparece na interface e no card
    /// compartilhável. O front-end recebe pronto e não precisa manter a própria
    /// tabela de tradução.
    /// </summary>
    public static string NomeDeExibicao(Habilidade habilidade) =>
        NomesDeExibicao.TryGetValue(habilidade, out var nome) ? nome : habilidade.ToString();
}
