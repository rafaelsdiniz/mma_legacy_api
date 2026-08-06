using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Simulation;

namespace MmaLegacy.Api.Services;

/// <summary>
/// Conduz o draft: apresenta o atleta da vez e registra a escolha do jogador.
/// </summary>
public sealed class ServicoDeDraft(ContextoDoJogo contexto, ServicoDePartida servicoDePartida)
{
    /// <summary>
    /// O atleta da rodada atual, com todas as suas notas e as habilidades que
    /// ainda podem ser escolhidas.
    /// </summary>
    public async Task<RodadaEmAberto> ObterRodadaAtualAsync(
        Guid partidaId,
        CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var rodada = partida.RodadaAtual();
        var atleta = await CarregarAtletaAsync(rodada.LutadorId, cancelamento);

        return new RodadaEmAberto(partida, rodada, atleta);
    }

    /// <summary>
    /// Registra a habilidade escolhida e avança o draft.
    /// </summary>
    /// <remarks>
    /// O jogador manda apenas o par atleta + habilidade. A nota vem do acervo
    /// carregado aqui, e todas as validações — se é a vez daquele atleta, se a
    /// habilidade continua livre — acontecem dentro de
    /// <see cref="Partida.EscolherHabilidade"/>. O serviço não decide nada:
    /// só busca o que o domínio precisa para decidir.
    /// </remarks>
    public async Task<Partida> EscolherAsync(
        Guid partidaId,
        Guid atletaId,
        Habilidade habilidade,
        CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var atleta = await CarregarAtletaAsync(atletaId, cancelamento);

        partida.EscolherHabilidade(atleta, habilidade);
        await contexto.SaveChangesAsync(cancelamento);

        return partida;
    }

    /// <summary>
    /// Dispensa o atleta da rodada e põe outro no lugar.
    /// </summary>
    /// <remarks>
    /// É a válvula de escape de quem recebeu alguém que não serve para nada do
    /// que falta montar. O domínio decide se ainda há pulo disponível e recusa
    /// repetir alguém já sorteado; ao serviço cabe só encontrar um substituto,
    /// porque quem conhece o acervo é ele.
    /// <para>
    /// O substituto é sorteado com a semente da partida somada ao número de
    /// pulos já usados. Assim o pulo continua reproduzível — a mesma partida
    /// pulando na mesma rodada recebe sempre o mesmo atleta — sem que o segundo
    /// pulo repita o sorteio do primeiro.
    /// </para>
    /// </remarks>
    public async Task<Partida> PularAsync(Guid partidaId, CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var substituto = await SortearSubstitutoAsync(partida, cancelamento);

        partida.PularAtleta(substituto);
        await contexto.SaveChangesAsync(cancelamento);

        return partida;
    }

    private async Task<Lutador> SortearSubstitutoAsync(Partida partida, CancellationToken cancelamento)
    {
        var jaSorteados = partida.AtletasJaSorteados();

        var disponiveis = await contexto.Lutadores
            .Where(lutador => lutador.SorteavelNoDraft && !jaSorteados.Contains(lutador.Id))
            .OrderBy(lutador => lutador.Slug)
            .ToListAsync(cancelamento);

        RegraDeNegocioException.Se(
            disponiveis.Count == 0,
            "Não há mais atletas no elenco do draft para substituir este.");

        return new Sorteio(unchecked(partida.Seed + partida.PulosUsados + 1)).Escolher(disponiveis);
    }

    private async Task<Lutador> CarregarAtletaAsync(Guid atletaId, CancellationToken cancelamento) =>
        await contexto.Lutadores.FirstOrDefaultAsync(lutador => lutador.Id == atletaId, cancelamento)
        ?? throw RecursoNaoEncontradoException.Para("Atleta", atletaId);

    /// <summary>Estado do draft no momento em que o jogador precisa decidir.</summary>
    public sealed record RodadaEmAberto(Partida Partida, RodadaDeDraft Rodada, Lutador Atleta);
}
