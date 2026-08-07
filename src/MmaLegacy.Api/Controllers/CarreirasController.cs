using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>
/// A carreira jogada: estreia, ofertas de luta e o desfecho de cada decisão.
/// </summary>
/// <remarks>
/// Todas as jogadas devolvem a mesma <see cref="SituacaoDaCarreiraResposta"/>.
/// A tela não precisa saber qual endpoint chamou para saber o que desenhar:
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
    /// Idempotente: chamar de novo devolve a carreira que já está em andamento,
    /// sem reiniciar nada.
    /// </remarks>
    /// <response code="200">Carreira em andamento, com as ofertas na mesa.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">O draft ainda não foi concluído.</response>
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

    /// <summary>Devolve a situação atual da carreira, sem alterar nada.</summary>
    /// <response code="200">Situação da carreira.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">O lutador ainda não estreou.</response>
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
    /// Aceita uma das ofertas na mesa. A luta é simulada round a round e vem
    /// junto na resposta.
    /// </summary>
    /// <response code="200">Luta disputada e carreira atualizada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">Não há essa oferta na mesa, ou a carreira já acabou.</response>
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
    /// Não é de graça: consome o mesmo espaço de calendário que uma luta e apaga
    /// o progresso rumo à promoção. Três recusas seguidas e a organização
    /// dispensa o lutador.
    /// </remarks>
    /// <response code="200">Rodada recusada e carreira atualizada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">Não há ofertas na mesa, ou a carreira já acabou.</response>
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

    /// <summary>Pendura as luvas por vontade própria e fecha o veredito de legado.</summary>
    /// <response code="200">Carreira encerrada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira já estava encerrada.</response>
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
    /// Entrega a carreira ao jogador automático, que a leva do ponto atual até a
    /// aposentadoria.
    /// </summary>
    /// <response code="200">Carreira simulada até o fim.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira já estava encerrada.</response>
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
