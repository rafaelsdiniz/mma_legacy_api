using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>
/// A carreira jogada: estreia, ofertas de luta e o desfecho de cada decisÃ£o.
/// </summary>
/// <remarks>
/// Todas as jogadas devolvem a mesma <see cref="SituacaoDaCarreiraResposta"/>.
/// A tela nÃ£o precisa saber qual endpoint chamou para saber o que desenhar:
/// aceitar, recusar, aposentar e simular o resto entregam o mesmo retrato do
/// mundo depois da jogada.
/// </remarks>
[ApiController]
[Route("api/partidas/{partidaId:guid}/carreira")]
[Produces("application/json")]
public sealed class CarreirasController(ServicoDeCarreira servicoDeCarreira) : ControllerBase
{
    /// <summary>
    /// Estreia o lutador e devolve a primeira rodada de ofertas.
    /// </summary>
    /// <remarks>
    /// Idempotente: chamar de novo devolve a carreira que jÃ¡ estÃ¡ em andamento,
    /// sem reiniciar nada.
    /// </remarks>
    /// <response code="200">Carreira em andamento, com as ofertas na mesa.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">O draft ainda nÃ£o foi concluÃ­do.</response>
    [HttpPost("estrear")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Estrear(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.EstrearAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>Devolve a situaÃ§Ã£o atual da carreira, sem alterar nada.</summary>
    /// <response code="200">SituaÃ§Ã£o da carreira.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">O lutador ainda nÃ£o estreou.</response>
    [HttpGet]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Obter(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.ObterAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>
    /// Aceita uma das ofertas na mesa. A luta Ã© simulada round a round e vem
    /// junto na resposta.
    /// </summary>
    /// <response code="200">Luta disputada e carreira atualizada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">NÃ£o hÃ¡ essa oferta na mesa, ou a carreira jÃ¡ acabou.</response>
    [HttpPost("aceitar")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Aceitar(
        Guid partidaId,
        [FromBody] AceitarOfertaRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.AceitarAsync(
            partidaId,
            requisicao.Indice,
            requisicao.FocoDoCamp,
            requisicao.Intensidade,
            cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>
    /// Recusa a rodada inteira de ofertas.
    /// </summary>
    /// <remarks>
    /// NÃ£o Ã© de graÃ§a: consome o mesmo espaÃ§o de calendÃ¡rio que uma luta e apaga
    /// o progresso rumo Ã  promoÃ§Ã£o. TrÃªs recusas seguidas e a organizaÃ§Ã£o
    /// dispensa o lutador.
    /// </remarks>
    /// <response code="200">Rodada recusada e carreira atualizada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">NÃ£o hÃ¡ ofertas na mesa, ou a carreira jÃ¡ acabou.</response>
    [HttpPost("recusar")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Recusar(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.RecusarAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>Pendura as luvas por vontade prÃ³pria e fecha o veredito de legado.</summary>
    /// <response code="200">Carreira encerrada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira jÃ¡ estava encerrada.</response>
    [HttpPost("aposentar")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Aposentar(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.AposentarAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>
    /// Entrega a carreira ao jogador automÃ¡tico, que a leva do ponto atual atÃ© a
    /// aposentadoria.
    /// </summary>
    /// <response code="200">Carreira simulada atÃ© o fim.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira jÃ¡ estava encerrada.</response>
    [HttpPost("simular-o-resto")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> SimularOResto(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.SimularORestoAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>
    /// Passa um compromisso do calendário tratando a lesão.
    /// </summary>
    /// <remarks>
    /// É a única jogada possível enquanto o lutador está machucado: a mesa de
    /// ofertas fica vazia até ele se recuperar.
    /// </remarks>
    /// <response code="200">Um compromisso de recuperação passou.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira já acabou, ou não há lesão para tratar.</response>
    [HttpPost("recuperar")]
    [ProducesResponseType<SituacaoDaCarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SituacaoDaCarreiraResposta>> Recuperar(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var jogada = await servicoDeCarreira.RecuperarAsync(partidaId, cancelamento);

        return Ok(Responder(jogada));
    }

    /// <summary>
    /// Monta a resposta a partir da jogada.
    /// </summary>
    /// <remarks>
    /// Todas as ações devolvem o mesmo retrato do mundo, e é isso que permite à
    /// tela redesenhar sem saber qual endpoint chamou.
    /// </remarks>
    private static SituacaoDaCarreiraResposta Responder(JogadaDaCarreira jogada) =>
        SituacaoDaCarreiraResposta.DeDominio(
            jogada.Partida,
            jogada.Tabela,
            jogada.Passo,
            jogada.PosicaoAnterior);
}
