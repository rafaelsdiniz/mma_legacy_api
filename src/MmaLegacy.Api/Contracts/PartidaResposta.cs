using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Contracts;

/// <summary>Estado completo de uma partida.</summary>
public sealed record PartidaResposta(
    Guid Id,
    int Seed,
    DateTimeOffset CriadaEm,
    StatusDaPartida Status,
    NivelDeDificuldade NivelDeDificuldade,
    int EscolhasFeitas,
    int TotalDeRodadas,
    FichaResposta Ficha,
    LutadorMontadoResposta? Lutador)
{
    public static PartidaResposta DeDominio(Partida partida) => new(
        partida.Id,
        partida.Seed,
        partida.CriadaEm,
        partida.Status,
        partida.NivelDeDificuldade,
        partida.EscolhasFeitas,
        Habilidades.Quantidade,
        FichaResposta.DeDominio(partida.Ficha),
        partida.Lutador is null ? null : LutadorMontadoResposta.DeDominio(partida.Ficha, partida.Lutador));
}

/// <summary>A identidade do lutador, como informada no início da partida.</summary>
public sealed record FichaResposta(
    string Nome,
    string Apelido,
    string Nacionalidade,
    CategoriaDePeso CategoriaDePeso,
    string CategoriaDePesoTexto,
    int IdadeInicial,
    BaseDeLuta BaseDeLuta,
    string NomeDeCartaz)
{
    public static FichaResposta DeDominio(FichaDeInscricao ficha) => new(
        ficha.Nome,
        ficha.Apelido,
        ficha.Nacionalidade,
        ficha.CategoriaDePeso,
        Categorias.NomeDeExibicao(ficha.CategoriaDePeso),
        ficha.IdadeInicial,
        ficha.BaseDeLuta,
        ficha.NomeDeCartaz());
}

/// <summary>O lutador que saiu do draft, pronto para a tela de revelação.</summary>
public sealed record LutadorMontadoResposta(
    string NomeDeCartaz,
    string CategoriaDePesoTexto,
    decimal Overall,
    EstiloDeLuta Estilo,
    IReadOnlyList<NotaDeHabilidadeResposta> Atributos,
    string MaiorQualidade,
    string PrincipalFraqueza)
{
    public static LutadorMontadoResposta DeDominio(FichaDeInscricao ficha, LutadorCriado lutador) => new(
        ficha.NomeDeCartaz(),
        Categorias.NomeDeExibicao(ficha.CategoriaDePeso),
        lutador.Overall,
        lutador.Estilo,
        NotaDeHabilidadeResposta.DeAtributos(lutador.Atributos),
        Habilidades.NomeDeExibicao(lutador.MaiorQualidade),
        Habilidades.NomeDeExibicao(lutador.PrincipalFraqueza));
}

/// <summary>
/// Uma habilidade com sua nota.
/// </summary>
/// <remarks>
/// O nome acentuado vem pronto do servidor para o front-end não precisar
/// manter a própria tabela de tradução dos enums.
/// </remarks>
public sealed record NotaDeHabilidadeResposta(Habilidade Habilidade, string Nome, int Nota)
{
    public static NotaDeHabilidadeResposta DeDominio(KeyValuePair<Habilidade, int> par) =>
        new(par.Key, Habilidades.NomeDeExibicao(par.Key), par.Value);

    /// <summary>As oito notas de um conjunto de atributos, na ordem do draft.</summary>
    public static IReadOnlyList<NotaDeHabilidadeResposta> DeAtributos(Atributos atributos)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        return atributos.Listar().Select(DeDominio).ToList();
    }
}
