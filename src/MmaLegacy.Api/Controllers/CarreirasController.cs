using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>Simulação e consulta da carreira do lutador montado.</summary>
[ApiController]
[Route("api/partidas/{partidaId:guid}")]
[Produces("application/json")]
public sealed class CarreirasController(
    ServicoDeCarreira servicoDeCarreira,
    ServicoDePartida servicoDePartida) : ControllerBase
{
    /// <summary>
    /// Simula a carreira inteira do lutador montado.
    /// </summary>
    /// <remarks>
    /// A operação é idempotente: chamar de novo devolve a mesma carreira, sem
    /// re-simular. Como a semente é fixa, o resultado seria idêntico de qualquer
    /// forma, e assim uma tela recarregada não parece sortear outra vida.
    /// </remarks>
    /// <response code="200">Carreira simulada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">O draft ainda não foi concluído.</response>
    [HttpPost("carreira/simular")]
    [ProducesResponseType<CarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarreiraResposta>> Simular(Guid partidaId, CancellationToken cancelamento)
    {
        var carreira = await servicoDeCarreira.SimularAsync(partidaId, cancelamento);

        return Ok(CarreiraResposta.DeDominio(carreira));
    }

    /// <summary>Devolve a carreira já simulada.</summary>
    /// <response code="200">Carreira encontrada.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira desta partida ainda não foi simulada.</response>
    [HttpGet("carreira")]
    [ProducesResponseType<CarreiraResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarreiraResposta>> ObterCarreira(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var carreira = await servicoDeCarreira.ObterAsync(partidaId, cancelamento);

        return Ok(CarreiraResposta.DeDominio(carreira));
    }

    /// <summary>
    /// O pacote completo do fim de jogo: ficha, lutador montado e carreira.
    /// </summary>
    /// <remarks>
    /// É o endpoint da tela de resultado e do card compartilhável, para que a
    /// imagem seja gerada a partir de uma leitura só e não de três.
    /// </remarks>
    /// <response code="200">Resultado completo.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A partida ainda não chegou ao fim.</response>
    [HttpGet("resultado")]
    [ProducesResponseType<ResultadoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultadoResposta>> ObterResultado(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);

        return Ok(ResultadoResposta.DeDominio(partida));
    }
}
