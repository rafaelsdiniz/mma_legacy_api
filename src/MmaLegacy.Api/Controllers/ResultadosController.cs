using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>O pacote de fim de jogo: ficha, lutador montado e carreira encerrada.</summary>
[ApiController]
[Route("api/partidas/{partidaId:guid}")]
[Produces("application/json")]
public sealed class ResultadosController(ServicoDePartida servicoDePartida) : ControllerBase
{
    /// <summary>
    /// O pacote completo do fim de jogo: ficha, lutador montado e carreira.
    /// </summary>
    /// <remarks>
    /// É o endpoint da tela de resultado e do card compartilhável, para que a
    /// imagem seja gerada a partir de uma leitura só e não de três.
    /// </remarks>
    /// <response code="200">Resultado completo.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">A carreira ainda está em andamento.</response>
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
