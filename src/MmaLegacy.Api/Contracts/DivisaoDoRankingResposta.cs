using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// Uma divisão com seu campeão e os quinze ranqueados, na ordem do ranking.
/// </summary>
/// <remarks>
/// O campeão vem separado dos demais de propósito. Na tabela ele não é "a
/// posição zero" — é outra coisa, com destaque próprio, e separar aqui evita
/// que o front-end tenha que filtrar a lista para desenhar isso.
/// </remarks>
public sealed record DivisaoDoRankingResposta(
    CategoriaDePeso Categoria,
    string CategoriaTexto,
    LutadorDoAcervoResposta? Campeao,
    IReadOnlyList<LutadorDoAcervoResposta> Ranqueados)
{
    public static DivisaoDoRankingResposta DeDominio(
        CategoriaDePeso categoria,
        IEnumerable<Lutador> atletas)
    {
        var emOrdem = atletas.OrderBy(atleta => atleta.PosicaoNoRanking).ToList();

        return new DivisaoDoRankingResposta(
            categoria,
            Categorias.NomeDeExibicao(categoria),
            emOrdem.FirstOrDefault(atleta => atleta.EhCampeao) is { } campeao
                ? LutadorDoAcervoResposta.DeDominio(campeao)
                : null,
            emOrdem
                .Where(atleta => !atleta.EhCampeao)
                .Select(LutadorDoAcervoResposta.DeDominio)
                .ToList());
    }
}
