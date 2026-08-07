using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;
using MmaLegacy.Api.Simulation;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// A tela da carreira jogada: onde o lutador está, o que aconteceu na última
/// decisão e o que a organização está oferecendo agora.
/// </summary>
/// <remarks>
/// É uma resposta só de propósito. Toda jogada muda estado, cartel e ofertas ao
/// mesmo tempo, e devolver as três coisas juntas evita que a tela pisque com
/// dados de momentos diferentes — o jogador veria o cartel novo ao lado da
/// oferta velha.
/// </remarks>
public sealed record SituacaoDaCarreiraResposta(
    Guid PartidaId,
    string NomeDeCartaz,
    bool Encerrada,
    MotivoDoEncerramento? MotivoDoEncerramento,
    EstadoDaCarreiraResposta Estado,
    IReadOnlyList<OfertaDeLutaResposta> Ofertas,
    CarreiraResposta Carreira,
    DesfechoDaUltimaLutaResposta? UltimaLuta,
    IReadOnlyList<EventoDaCarreira> Eventos,
    IReadOnlyList<LinhaDoRankingResposta> RankingDaDivisao,
    int? PosicaoAnterior)
{
    /// <param name="tabela">
    /// O ranking real da divisão do jogador. Vem de fora porque quem conhece o
    /// acervo é o serviço; a resposta só encaixa o jogador nele.
    /// </param>
    /// <param name="posicaoAnterior">
    /// Onde o jogador estava antes desta jogada, para a tela poder animar o
    /// movimento. Nulo quando nada mudou.
    /// </param>
    public static SituacaoDaCarreiraResposta DeDominio(
        Partida partida,
        TabelaDaDivisao? tabela = null,
        PassoDaCarreira? passo = null,
        int? posicaoAnterior = null)
    {
        ArgumentNullException.ThrowIfNull(partida);

        var carreira = partida.ExigirCarreira();
        var nome = partida.Ficha.NomeDeCartaz();

        return new SituacaoDaCarreiraResposta(
            partida.Id,
            nome,
            carreira.Encerrada,
            carreira.MotivoDoEncerramento,
            EstadoDaCarreiraResposta.DeDominio(carreira),
            carreira.Ofertas
                .Select(oferta => OfertaDeLutaResposta.DeDominio(oferta, carreira.Estado))
                .ToList(),
            CarreiraResposta.DeDominio(carreira),
            DesfechoDaUltimaLutaResposta.DeDominio(passo),
            passo?.Eventos ?? [],
            MontarRanking(tabela, nome, carreira.Estado.PosicaoNoRanking),
            posicaoAnterior);
    }

    private static IReadOnlyList<LinhaDoRankingResposta> MontarRanking(
        TabelaDaDivisao? tabela,
        string nome,
        int? posicaoDoJogador) =>
        tabela is null || tabela.EstaVazia
            ? []
            : tabela.ComOJogador(nome, posicaoDoJogador)
                .Select(LinhaDoRankingResposta.DeDominio)
                .ToList();
}

/// <summary>Uma linha da tabela do ranking, já com o jogador encaixado nela.</summary>
public sealed record LinhaDoRankingResposta(
    int Posicao,
    string Nome,
    string? Slug,
    decimal Overall,
    bool EhOJogador)
{
    public static LinhaDoRankingResposta DeDominio(LinhaDoRanking linha) => new(
        linha.Posicao,
        linha.Nome,
        linha.Slug,
        linha.Overall,
        linha.EhOJogador);
}

/// <summary>Onde o lutador está agora e a que distância está do próximo degrau.</summary>
/// <remarks>
/// Os limiares vêm junto com os contadores — "2 de 3 vitórias para subir",
/// "faltam 2 derrotas para ser dispensado". Mandar só o contador obrigaria a
/// tela a conhecer as regras do jogo, e um dia as duas versões da regra
/// discordariam.
/// </remarks>
public sealed record EstadoDaCarreiraResposta(
    int Idade,
    CategoriaDePeso Categoria,
    string CategoriaTexto,
    EtapaDaCarreira Etapa,
    NivelDaOrganizacao Organizacao,
    EstiloDeLuta Estilo,
    decimal OverallAtual,
    decimal OverallMaximo,
    IReadOnlyList<NotaDeHabilidadeResposta> Atributos,
    bool EhCampeao,
    int SequenciaDeVitorias,
    int DerrotasSeguidas,
    int DerrotasParaSerDispensado,
    int RecusasSeguidas,
    int RecusasParaSerDispensado,
    int VitoriasNaEtapa,
    int VitoriasParaSubir,
    int CompromissosNaTemporada,
    int CompromissosPorTemporada,
    int VezesDispensado,
    int LesoesSofridas,
    LesaoResposta? Lesao,
    int? PosicaoNoRanking)
{
    public static EstadoDaCarreiraResposta DeDominio(Carreira carreira)
    {
        var estado = carreira.Estado;

        return new EstadoDaCarreiraResposta(
            estado.Idade,
            estado.Categoria,
            Categorias.NomeDeExibicao(estado.Categoria),
            estado.Etapa,
            RegrasDaCarreira.OrganizacaoDe(estado.Etapa),
            estado.Estilo,
            estado.OverallAtual,
            estado.OverallMaximo,
            NotaDeHabilidadeResposta.DeAtributos(estado.Atributos),
            estado.EhCampeao,
            carreira.SequenciaAtualDeVitorias,
            estado.DerrotasSeguidas,
            RegrasDaCarreira.DerrotasParaSerDispensado,
            estado.RecusasSeguidas,
            RegrasDaCarreira.RecusasParaSerDispensado,
            estado.VitoriasNaEtapa,
            RegrasDaCarreira.VitoriasParaSubir(estado.Etapa),
            estado.CompromissosNaTemporada,
            RegrasDaCarreira.CompromissosPorTemporada(estado.Etapa),
            estado.VezesDispensado,
            estado.LesoesSofridas,
            LesaoResposta.DeDominio(estado.LesaoAtual),
            estado.PosicaoNoRanking);
    }
}

/// <summary>
/// A lesão que está tirando o lutador de ação.
/// </summary>
/// <remarks>
/// Vem com a sequela já explicitada, porque o jogador precisa entender o que
/// perdeu. "Joelho lesionado" é uma frase; "joelho lesionado, dois pontos de
/// velocidade a menos, para sempre" é a regra do jogo — e é ela que faz o
/// jogador pensar duas vezes na próxima oferta brutal.
/// </remarks>
public sealed record LesaoResposta(
    TipoDeLesao Tipo,
    GravidadeDaLesao Gravidade,
    Habilidade? HabilidadeAfetada,
    int PontosPerdidos,
    int Afastamento,
    int CompromissosRestantes,
    int IdadeQuandoOcorreu)
{
    public static LesaoResposta? DeDominio(Lesao? lesao)
    {
        if (lesao is null)
        {
            return null;
        }

        var pontosPerdidos = Lesao.SequelaDe(lesao.Gravidade);

        return new LesaoResposta(
            lesao.Tipo,
            lesao.Gravidade,
            // Corte não deixa sequela, e apontar uma habilidade afetada com
            // zero ponto perdido faria a tela mentir.
            pontosPerdidos > 0 ? Lesao.HabilidadeAfetadaPor(lesao.Tipo) : null,
            pontosPerdidos,
            lesao.Afastamento,
            lesao.CompromissosRestantes,
            lesao.IdadeQuandoOcorreu);
    }
}

/// <summary>Uma luta na mesa, esperando a decisão do jogador.</summary>
public sealed record OfertaDeLutaResposta(
    int Indice,
    string Adversario,
    string CartelDoAdversario,
    decimal OverallDoAdversario,
    EstiloDeLuta EstiloDoAdversario,
    IReadOnlyList<NotaDeHabilidadeResposta> AtributosDoAdversario,
    NivelDaOrganizacao Organizacao,
    CategoriaDePeso Categoria,
    string CategoriaTexto,
    bool ValendoCinturao,
    bool DisputaDeCinturao,
    bool DefesaDeCinturao,
    int RoundsProgramados,
    string Chamada,
    GrauDeDificuldade Dificuldade,
    double RiscoDeLesao,
    string? SlugDoAdversario,
    int? PosicaoDoAdversario)
{
    public static OfertaDeLutaResposta DeDominio(OfertaDeLuta oferta, EstadoDaCarreira estado) => new(
        oferta.Indice,
        oferta.Adversario,
        oferta.CartelDoAdversario,
        oferta.OverallDoAdversario,
        oferta.EstiloDoAdversario,
        NotaDeHabilidadeResposta.DeAtributos(oferta.AtributosDoAdversario),
        oferta.Organizacao,
        oferta.Categoria,
        Categorias.NomeDeExibicao(oferta.Categoria),
        oferta.ValendoCinturao,
        oferta.DisputaDeCinturao,
        oferta.DefesaDeCinturao,
        oferta.RoundsProgramados,
        oferta.Chamada,
        oferta.DificuldadeContra(estado.OverallAtual),
        oferta.RiscoDeLesaoPara(estado),
        oferta.SlugDoAdversario,
        oferta.PosicaoDoAdversario);
}

/// <summary>
/// A luta que acabou de acontecer, com o round a round que o motor produziu.
/// </summary>
/// <remarks>
/// Só existe na resposta da jogada que a gerou. O detalhe de cada round é
/// narrativa do momento, não histórico: guardar cinco rounds para cada uma das
/// trinta lutas de uma carreira custaria caro por algo que ninguém relê.
/// </remarks>
public sealed record DesfechoDaUltimaLutaResposta(
    LutaResposta Luta,
    IReadOnlyList<RoundResposta> Rounds)
{
    public static DesfechoDaUltimaLutaResposta? DeDominio(PassoDaCarreira? passo) =>
        passo?.Luta is { } luta && passo.Desfecho is { } desfecho
            ? new DesfechoDaUltimaLutaResposta(
                LutaResposta.DeDominio(luta),
                desfecho.Rounds.Select(RoundResposta.DeDominio).ToList())
            : null;
}

/// <summary>Um round da luta, do ponto de vista do lutador do jogador.</summary>
public sealed record RoundResposta(
    int Numero,
    VencedorDoRound Vencedor,
    bool LutadorBuscouQueda,
    bool LutadorControlou,
    bool AdversarioBuscouQueda,
    bool AdversarioControlou,
    int FadigaDoLutador,
    int FadigaDoAdversario,
    int DanoDoLutador,
    int DanoDoAdversario,
    MetodoDeEncerramento? Encerramento)
{
    public static RoundResposta DeDominio(RoundDaLuta round) => new(
        round.Numero,
        round.Vencedor,
        round.LutadorBuscouQueda,
        round.LutadorControlou,
        round.AdversarioBuscouQueda,
        round.AdversarioControlou,
        round.FadigaDoLutador,
        round.FadigaDoAdversario,
        round.DanoDoLutador,
        round.DanoDoAdversario,
        round.Encerramento);
}

/// <summary>Corpo do pedido de aceitar uma oferta.</summary>
public sealed record AceitarOfertaRequisicao(int Indice);
