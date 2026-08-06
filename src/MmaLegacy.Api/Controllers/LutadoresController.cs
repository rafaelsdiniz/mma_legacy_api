using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Contracts;
using MmaLegacy.Api.Data;

namespace MmaLegacy.Api.Controllers;

/// <summary>O acervo de atletas que alimenta o draft.</summary>
[ApiController]
[Route("api/lutadores")]
[Produces("application/json")]
public sealed class LutadoresController(ContextoDoJogo contexto) : ControllerBase
{
    /// <summary>
    /// Lista todos os atletas do acervo com suas notas.
    /// </summary>
    /// <remarks>
    /// É leitura pública e o acervo muda só quando o jogo é rebalanceado, então
    /// a resposta pode ser cacheada com folga. Sem isso, cada visita à página de
    /// lutadores acordaria a instância e consultaria o banco à toa.
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<IReadOnlyList<LutadorDoAcervoResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LutadorDoAcervoResposta>>> Listar(
        CancellationToken cancelamento)
    {
        var acervo = await contexto.Lutadores
            .AsNoTracking()
            .OrderBy(lutador => lutador.Nome)
            .ToListAsync(cancelamento);

        return Ok(acervo.Select(LutadorDoAcervoResposta.DeDominio).ToList());
    }

    /// <summary>
    /// Devolve o ranking das divisões, da mais leve para a mais pesada.
    /// </summary>
    /// <remarks>
    /// É a mesma escada que a carreira percorre. Lendas ficam de fora: elas
    /// entram no draft, mas não competem no presente.
    /// </remarks>
    [HttpGet("ranking")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<IReadOnlyList<DivisaoDoRankingResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DivisaoDoRankingResposta>>> Ranking(
        CancellationToken cancelamento)
    {
        var ranqueados = await contexto.Lutadores
            .AsNoTracking()
            .Where(lutador => !lutador.EhLenda
                && lutador.Categoria != null
                && lutador.PosicaoNoRanking != null)
            .OrderBy(lutador => lutador.Categoria)
            .ThenBy(lutador => lutador.PosicaoNoRanking)
            .ToListAsync(cancelamento);

        var divisoes = ranqueados
            .GroupBy(lutador => lutador.Categoria!.Value)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => DivisaoDoRankingResposta.DeDominio(grupo.Key, grupo))
            .ToList();

        return Ok(divisoes);
    }
}
