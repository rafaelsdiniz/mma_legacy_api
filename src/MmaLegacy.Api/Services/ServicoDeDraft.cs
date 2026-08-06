using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

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

    private async Task<Lutador> CarregarAtletaAsync(Guid atletaId, CancellationToken cancelamento) =>
        await contexto.Lutadores.FirstOrDefaultAsync(lutador => lutador.Id == atletaId, cancelamento)
        ?? throw RecursoNaoEncontradoException.Para("Atleta", atletaId);

    /// <summary>Estado do draft no momento em que o jogador precisa decidir.</summary>
    public sealed record RodadaEmAberto(Partida Partida, RodadaDeDraft Rodada, Lutador Atleta);
}
