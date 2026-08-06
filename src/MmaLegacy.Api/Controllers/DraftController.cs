using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Services;

namespace MmaLegacy.Api.Controllers;

/// <summary>Condução do draft de uma partida.</summary>
[ApiController]
[Route("api/partidas/{partidaId:guid}/draft")]
[Produces("application/json")]
public sealed class DraftController(ServicoDeDraft servicoDeDraft) : ControllerBase
{
    /// <summary>
    /// O atleta da rodada atual, suas notas e as habilidades ainda disponíveis.
    /// </summary>
    /// <response code="200">Rodada em aberto.</response>
    /// <response code="404">Partida ou atleta inexistente.</response>
    /// <response code="409">O draft desta partida já foi concluído.</response>
    [HttpGet("atual")]
    [ProducesResponseType<RodadaAtualResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RodadaAtualResposta>> ObterRodadaAtual(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        var rodada = await servicoDeDraft.ObterRodadaAtualAsync(partidaId, cancelamento);

        return Ok(RodadaAtualResposta.DeDominio(rodada));
    }

    /// <summary>
    /// Registra a habilidade escolhida na rodada atual.
    /// </summary>
    /// <remarks>
    /// Na oitava escolha o draft se encerra e o lutador é montado — a resposta
    /// já vem com <c>lutador</c> preenchido e o status em <c>DraftConcluido</c>.
    /// </remarks>
    /// <response code="200">Escolha registrada.</response>
    /// <response code="400">Requisição sem atleta ou sem habilidade.</response>
    /// <response code="404">Partida ou atleta inexistente.</response>
    /// <response code="409">
    /// Não é a vez daquele atleta, a habilidade já foi preenchida ou o draft já acabou.
    /// </response>
    [HttpPost("escolher")]
    [ProducesResponseType<PartidaResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartidaResposta>> Escolher(
        Guid partidaId,
        [FromBody] EscolherHabilidadeRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var partida = await servicoDeDraft.EscolherAsync(
            partidaId,
            requisicao.AtletaId,
            requisicao.Habilidade!.Value,
            cancelamento);

        return Ok(PartidaResposta.DeDominio(partida));
    }

    /// <summary>
    /// Dispensa o atleta da rodada atual e sorteia outro no lugar.
    /// </summary>
    /// <remarks>
    /// O jogador tem dois pulos no modo fácil e um no difícil. É a saída para
    /// quando o atleta da vez não tem nada que sirva ao que falta montar — e é
    /// escasso de propósito, para continuar sendo uma decisão.
    /// <para>
    /// A resposta é a rodada nova, e não a partida, porque o front precisa
    /// justamente do substituto para redesenhar a tela.
    /// </para>
    /// </remarks>
    /// <response code="200">Atleta substituído.</response>
    /// <response code="404">Partida inexistente.</response>
    /// <response code="409">Os pulos acabaram ou o draft já foi concluído.</response>
    [HttpPost("pular")]
    [ProducesResponseType<RodadaAtualResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RodadaAtualResposta>> Pular(
        Guid partidaId,
        CancellationToken cancelamento)
    {
        await servicoDeDraft.PularAsync(partidaId, cancelamento);

        var rodada = await servicoDeDraft.ObterRodadaAtualAsync(partidaId, cancelamento);

        return Ok(RodadaAtualResposta.DeDominio(rodada));
    }
}
