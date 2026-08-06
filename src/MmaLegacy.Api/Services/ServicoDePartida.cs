using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Simulation;

namespace MmaLegacy.Api.Services;

/// <summary>
/// Abre partidas e as recupera do banco.
/// </summary>
public sealed class ServicoDePartida(ContextoDoJogo contexto)
{
    /// <summary>
    /// Cria uma partida com o draft já sorteado.
    /// </summary>
    /// <param name="ficha">Identidade do lutador informada pelo jogador.</param>
    /// <param name="seed">
    /// Semente da partida. Quando omitida, é sorteada. Informá-la permite
    /// reproduzir uma partida específica e é o que sustenta o draft diário.
    /// </param>
    /// <param name="nivelDeDificuldade">
    /// Se as notas aparecem durante o draft. Não altera a simulação: dois
    /// lutadores idênticos montados em níveis diferentes têm a mesma carreira.
    /// </param>
    public async Task<Partida> CriarAsync(
        FichaDeInscricao ficha,
        int? seed,
        NivelDeDificuldade nivelDeDificuldade = NivelDeDificuldade.Facil,
        CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(ficha);

        var sementeDaPartida = seed ?? Random.Shared.Next();
        var sorteados = await SortearAtletasAsync(sementeDaPartida, cancelamento);
        var partida = Partida.Iniciar(ficha, sementeDaPartida, sorteados, nivelDeDificuldade);

        contexto.Partidas.Add(partida);
        await contexto.SaveChangesAsync(cancelamento);

        return partida;
    }

    /// <summary>Carrega uma partida completa, com draft, lutador e carreira.</summary>
    /// <exception cref="RecursoNaoEncontradoException">Se a partida não existir.</exception>
    public async Task<Partida> ObterAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        await ConsultarCompletas().FirstOrDefaultAsync(partida => partida.Id == partidaId, cancelamento)
        ?? throw RecursoNaoEncontradoException.Para("Partida", partidaId);

    /// <summary>
    /// Consulta base das partidas.
    /// </summary>
    /// <remarks>
    /// Rodadas, ficha e lutador montado são tipos pertencidos e vêm sozinhos.
    /// A carreira é entidade separada e precisa do Include explícito — sem ele,
    /// a tela de resultado viria vazia sem nenhum erro, que é o pior tipo de bug.
    /// </remarks>
    private IQueryable<Partida> ConsultarCompletas() =>
        contexto.Partidas.Include(partida => partida.Carreira);

    /// <summary>
    /// Sorteia os oito atletas do draft.
    /// </summary>
    /// <remarks>
    /// A ordenação por slug antes de embaralhar não é decorativa: sem ela, a
    /// ordem em que o PostgreSQL devolve as linhas entraria no sorteio, e a
    /// mesma semente poderia render drafts diferentes depois de um
    /// <c>VACUUM</c> ou de uma reinserção do acervo. Aí o draft diário deixaria
    /// de ser igual para todo mundo.
    /// </remarks>
    private async Task<IReadOnlyList<Lutador>> SortearAtletasAsync(int seed, CancellationToken cancelamento)
    {
        // Só o elenco do draft entra no sorteio. O resto do acervo existe para o
        // ranking e para os adversários da carreira — sortear o décimo quarto
        // colocado de uma divisão transformaria a escolha do jogador em chute.
        var acervo = await contexto.Lutadores
            .Where(lutador => lutador.SorteavelNoDraft)
            .OrderBy(lutador => lutador.Slug)
            .ToListAsync(cancelamento);

        RegraDeNegocioException.Se(
            acervo.Count < Habilidades.Quantidade,
            $"O elenco do draft tem apenas {acervo.Count} atletas sorteáveis e o draft precisa de " +
            $"{Habilidades.Quantidade}. Rode o seed do banco antes de iniciar uma partida.");

        return new Sorteio(seed).Embaralhar(acervo).Take(Habilidades.Quantidade).ToList();
    }
}
