using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>Abertura e consulta de partidas.</summary>
[ApiController]
[Route("api/partidas")]
[Produces("application/json")]
public sealed class PartidasController(ServicoDePartida servicoDePartida) : ControllerBase
{
    /// <summary>Cria uma partida e sorteia os oito atletas do draft.</summary>
    /// <response code="201">Partida criada, com o draft pronto para começar.</response>
    /// <response code="400">A ficha de inscrição tem campos inválidos.</response>
    /// <response code="409">O acervo não tem atletas suficientes para montar um draft.</response>
    [HttpPost]
    [ProducesResponseType<PartidaResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartidaResposta>> Criar(
        [FromBody] CriarPartidaRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var partida = await servicoDePartida.CriarAsync(
            requisicao.ParaFicha(),
            requisicao.Seed,
            requisicao.NivelDeDificuldade ?? Domain.Enums.NivelDeDificuldade.Facil,
            cancelamento);

        return CreatedAtAction(
            nameof(Obter),
            new { partidaId = partida.Id },
            PartidaResposta.DeDominio(partida));
    }

    /// <summary>Devolve o estado atual de uma partida.</summary>
    /// <response code="200">Partida encontrada.</response>
    /// <response code="404">Não existe partida com esse identificador.</response>
    [HttpGet("{partidaId:guid}")]
    [ProducesResponseType<PartidaResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartidaResposta>> Obter(Guid partidaId, CancellationToken cancelamento)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);

        return Ok(PartidaResposta.DeDominio(partida));
    }
}
