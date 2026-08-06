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
    NivelDeDificuldade NivelDeDificuldade,
    AtletaDoDraftResposta Atleta,
    IReadOnlyList<Habilidade> HabilidadesDisponiveis,
    IReadOnlyList<EscolhaFeitaResposta> EscolhasFeitas)
{
    public static RodadaAtualResposta DeDominio(ServicoDeDraft.RodadaEmAberto rodadaEmAberto)
    {
        var nivel = rodadaEmAberto.Partida.NivelDeDificuldade;
        var mostrarNotas = nivel == NivelDeDificuldade.Facil;

        return new RodadaAtualResposta(
            rodadaEmAberto.Rodada.Ordem,
            Habilidades.Quantidade,
            nivel,
            AtletaDoDraftResposta.DeDominio(rodadaEmAberto.Atleta, mostrarNotas),
            rodadaEmAberto.Partida.HabilidadesDisponiveis(),
            rodadaEmAberto.Partida.Rodadas
                .Where(rodada => rodada.Concluida)
                .Select(rodada => EscolhaFeitaResposta.DeDominio(rodada, mostrarNotas))
                .ToList());
    }
}

/// <summary>
/// O atleta da vez.
/// </summary>
/// <remarks>
/// No modo difícil as notas saem da resposta <b>no servidor</b>, e não são
/// apenas escondidas na tela. Se viajassem pela rede, bastaria abrir a aba de
/// rede do navegador para o modo difícil virar o fácil — e o modo perderia
/// completamente o sentido.
/// <para>
/// O <c>Slug</c> vai junto porque é com ele que o front-end monta o caminho da
/// imagem em <c>public/fighters/</c>, sem normalizar o nome de novo no cliente.
/// </para>
/// </remarks>
public sealed record AtletaDoDraftResposta(
    Guid Id,
    string Nome,
    string Slug,
    string Pais,
    IReadOnlyList<NotaDeHabilidadeResposta> Notas)
{
    public static AtletaDoDraftResposta DeDominio(Lutador atleta, bool mostrarNotas) => new(
        atleta.Id,
        atleta.Nome,
        atleta.Slug,
        atleta.Pais,
        mostrarNotas
            ? atleta.Atributos.Listar().Select(NotaDeHabilidadeResposta.DeDominio).ToList()
            : []);
}

/// <summary>Uma escolha já registrada, para o painel de progresso do draft.</summary>
/// <remarks>
/// A nota é anulável porque no modo difícil o jogador não descobre o que pegou
/// antes de fechar as oito rodadas. O nome do atleta continua visível: lembrar
/// de quem veio cada habilidade é parte do que ele precisa para decidir.
/// </remarks>
public sealed record EscolhaFeitaResposta(
    int Ordem,
    Habilidade Habilidade,
    string HabilidadeNome,
    int? Nota,
    string AtletaNome)
{
    public static EscolhaFeitaResposta DeDominio(RodadaDeDraft rodada, bool mostrarNotas) => new(
        rodada.Ordem,
        rodada.HabilidadeEscolhida!.Value,
        Habilidades.NomeDeExibicao(rodada.HabilidadeEscolhida!.Value),
        mostrarNotas ? rodada.NotaObtida!.Value : null,
        rodada.LutadorNome);
}
