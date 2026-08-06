using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Contracts;

/// <summary>O resumo da carreira, na ordem em que a tela de resultado o exibe.</summary>
public sealed record CarreiraResposta(
    string Cartel,
    int Vitorias,
    int Derrotas,
    int Empates,
    int TotalDeLutas,
    int VitoriasPorNocaute,
    int VitoriasPorFinalizacao,
    int VitoriasPorDecisao,
    int IdadeDeEstreia,
    int IdadeDeAposentadoria,
    int MaiorSequenciaDeVitorias,
    decimal OverallMaximo,
    CategoriaDePeso CategoriaFinal,
    string CategoriaFinalTexto,
    NivelDeLegado Legado,
    int PontuacaoDeLegado,
    IReadOnlyList<ConquistaResposta> Conquistas,
    IReadOnlyList<LutaResposta> Lutas)
{
    public static CarreiraResposta DeDominio(Carreira carreira) => new(
        carreira.Cartel,
        carreira.Vitorias,
        carreira.Derrotas,
        carreira.Empates,
        carreira.TotalDeLutas,
        carreira.VitoriasPorNocaute,
        carreira.VitoriasPorFinalizacao,
        carreira.VitoriasPorDecisao,
        carreira.IdadeDeEstreia,
        carreira.IdadeDeAposentadoria,
        carreira.MaiorSequenciaDeVitorias,
        carreira.OverallMaximo,
        carreira.CategoriaFinal,
        Categorias.NomeDeExibicao(carreira.CategoriaFinal),
        carreira.Legado,
        carreira.PontuacaoDeLegado,
        MontarConquistas(carreira),
        carreira.Lutas.Select(LutaResposta.DeDominio).ToList());

    /// <summary>
    /// A lista de conquistas do resultado final, com as não alcançadas
    /// inclusive.
    /// </summary>
    /// <remarks>
    /// Mostrar o que <b>não</b> foi conquistado é parte do jogo: "✗ Carreira
    /// invicta" conta uma história que a ausência da linha não contaria. Por
    /// isso a lista traz todas as conquistas com um sinalizador, em vez de só
    /// as obtidas.
    /// </remarks>
    private static IReadOnlyList<ConquistaResposta> MontarConquistas(Carreira carreira)
    {
        var conquistas = new List<ConquistaResposta>
        {
            new($"Campeão dos {Categorias.NomeDeExibicao(carreira.CategoriaFinal).ToLowerInvariant()}s",
                carreira.FoiCampeao),
            new($"{carreira.DefesasDeCinturao} defesa(s) de cinturão", carreira.DefesasDeCinturao > 0),
            new("Duplo campeão", carreira.FoiDuploCampeao),
            new("Carreira invicta", carreira.AposentouInvicto),
            new($"{carreira.AnosComoCampeao} ano(s) como campeão", carreira.AnosComoCampeao > 0)
        };

        return conquistas;
    }
}

/// <summary>Uma conquista da carreira e se ela foi alcançada.</summary>
public sealed record ConquistaResposta(string Descricao, bool Alcancada);

/// <summary>Uma linha do cartel.</summary>
public sealed record LutaResposta(
    int Ordem,
    int Idade,
    string Adversario,
    decimal OverallDoAdversario,
    EstiloDeLuta EstiloDoAdversario,
    NivelDaOrganizacao Organizacao,
    CategoriaDePeso Categoria,
    bool ValendoCinturao,
    bool DisputaDeCinturao,
    bool DefesaDeCinturao,
    int RoundsProgramados,
    ResultadoDaLuta Resultado,
    MetodoDeEncerramento Metodo,
    int RoundDoEncerramento)
{
    public static LutaResposta DeDominio(LutaDaCarreira luta) => new(
        luta.Ordem,
        luta.Idade,
        luta.Adversario,
        luta.OverallDoAdversario,
        luta.EstiloDoAdversario,
        luta.Organizacao,
        luta.Categoria,
        luta.ValendoCinturao,
        luta.DisputaDeCinturao,
        luta.DefesaDeCinturao,
        luta.RoundsProgramados,
        luta.Resultado,
        luta.Metodo,
        luta.RoundDoEncerramento);
}
