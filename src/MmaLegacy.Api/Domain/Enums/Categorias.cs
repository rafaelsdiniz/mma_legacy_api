namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Utilitários sobre o enum <see cref="CategoriaDePeso"/>.
/// </summary>
public static class Categorias
{
    /// <summary>Todas as categorias, da mais leve para a mais pesada.</summary>
    public static readonly IReadOnlyList<CategoriaDePeso> Todas = Enum.GetValues<CategoriaDePeso>();

    private static readonly Dictionary<CategoriaDePeso, string> NomesDeExibicao = new()
    {
        [CategoriaDePeso.Mosca] = "Peso-mosca",
        [CategoriaDePeso.Galo] = "Peso-galo",
        [CategoriaDePeso.Pena] = "Peso-pena",
        [CategoriaDePeso.Leve] = "Peso-leve",
        [CategoriaDePeso.MeioMedio] = "Meio-médio",
        [CategoriaDePeso.Medio] = "Peso-médio",
        [CategoriaDePeso.MeioPesado] = "Meio-pesado",
        [CategoriaDePeso.Pesado] = "Peso-pesado"
    };

    public static string NomeDeExibicao(CategoriaDePeso categoria) =>
        NomesDeExibicao.TryGetValue(categoria, out var nome) ? nome : categoria.ToString();

    /// <summary>
    /// A categoria imediatamente acima, ou <c>null</c> se já for a mais pesada.
    /// É o caminho da mudança de divisão em busca do segundo cinturão.
    /// </summary>
    /// <remarks>
    /// Depende de os valores do enum serem sequenciais e ordenados por peso.
    /// Ao inserir uma categoria nova, mantenha a numeração contígua.
    /// </remarks>
    public static CategoriaDePeso? ProximaAcima(CategoriaDePeso categoria)
    {
        var proxima = categoria + 1;
        return Enum.IsDefined(proxima) ? proxima : null;
    }
}
