using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// Tudo que a tela do draft precisa para montar uma rodada: quem está na sua
/// frente, o que ele tem e o que você ainda pode pegar.
/// </summary>
public sealed record RodadaAtualResposta(
    int Ordem,
    int TotalDeRodadas,
    AtletaDoDraftResposta Atleta,
    IReadOnlyList<Habilidade> HabilidadesDisponiveis,
    IReadOnlyList<EscolhaFeitaResposta> EscolhasFeitas)
{
    public static RodadaAtualResposta DeDominio(ServicoDeDraft.RodadaEmAberto rodadaEmAberto) => new(
        rodadaEmAberto.Rodada.Ordem,
        Habilidades.Quantidade,
        AtletaDoDraftResposta.DeDominio(rodadaEmAberto.Atleta),
        rodadaEmAberto.Partida.HabilidadesDisponiveis(),
        rodadaEmAberto.Partida.Rodadas
            .Where(rodada => rodada.Concluida)
            .Select(EscolhaFeitaResposta.DeDominio)
            .ToList());
}

/// <summary>O atleta da vez, com todas as notas visíveis.</summary>
/// <remarks>
/// O <c>Slug</c> vai junto porque é com ele que o front-end monta o caminho da
/// imagem em <c>public/fighters/</c>, sem precisar normalizar o nome de novo do
/// lado do cliente.
/// </remarks>
public sealed record AtletaDoDraftResposta(
    Guid Id,
    string Nome,
    string Slug,
    string Pais,
    IReadOnlyList<NotaDeHabilidadeResposta> Notas)
{
    public static AtletaDoDraftResposta DeDominio(Lutador atleta) => new(
        atleta.Id,
        atleta.Nome,
        atleta.Slug,
        atleta.Pais,
        atleta.Atributos.Listar().Select(NotaDeHabilidadeResposta.DeDominio).ToList());
}

/// <summary>Uma escolha já registrada, para o painel de progresso do draft.</summary>
public sealed record EscolhaFeitaResposta(
    int Ordem,
    Habilidade Habilidade,
    string HabilidadeNome,
    int Nota,
    string AtletaNome)
{
    public static EscolhaFeitaResposta DeDominio(RodadaDeDraft rodada) => new(
        rodada.Ordem,
        rodada.HabilidadeEscolhida!.Value,
        Habilidades.NomeDeExibicao(rodada.HabilidadeEscolhida!.Value),
        rodada.NotaObtida!.Value,
        rodada.LutadorNome);
}
