using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Simulation;

namespace MmaLegacy.Api.Services;

/// <summary>
/// Conduz a carreira jogada: estreia o lutador, aplica cada decisão do jogador
/// e salva o resultado.
/// </summary>
/// <remarks>
/// O serviço não sabe nada de regra de MMA. Ele carrega a partida, chama o
/// movimento correspondente no <see cref="MotorDeCarreira"/> e persiste — toda
/// a decisão sobre o que aquilo significa para a carreira é do motor.
/// </remarks>
public sealed class ServicoDeCarreira(
    ContextoDoJogo contexto,
    ServicoDePartida servicoDePartida,
    MotorDeCarreira motorDeCarreira)
{
    /// <summary>
    /// Põe o lutador em atividade e coloca a primeira luta na mesa.
    /// </summary>
    /// <remarks>
    /// É idempotente: pedir de novo devolve a carreira que já existe. A tela da
    /// carreira chama isto ao abrir, e recarregar a página não pode significar
    /// sortear outra vida para o mesmo lutador.
    /// </remarks>
    public async Task<JogadaDaCarreira> EstrearAsync(
        Guid partidaId,
        CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var ranking = await CarregarRankingAsync(cancelamento);

        if (partida.Carreira is null)
        {
            var novaCarreira = motorDeCarreira.Iniciar(partida, ranking);

            partida.EstrearCarreira(novaCarreira);
            contexto.Carreiras.Add(novaCarreira);
            await contexto.SaveChangesAsync(cancelamento);
        }

        return Montar(partida, PassoDaCarreira.Vazio, ranking, posicaoAnterior: null);
    }

    /// <summary>O jogador aceita uma das ofertas na mesa e a luta acontece.</summary>
    /// <param name="indiceDaOferta">Índice da oferta escolhida, começando em 1.</param>
    public Task<JogadaDaCarreira> AceitarAsync(
        Guid partidaId,
        int indiceDaOferta,
        CancellationToken cancelamento = default) =>
        JogarAsync(
            partidaId,
            (partida, carreira, ranking) =>
                motorDeCarreira.Aceitar(partida, carreira, ranking, indiceDaOferta),
            cancelamento);

    /// <summary>O jogador recusa a rodada inteira e fica parado.</summary>
    public Task<JogadaDaCarreira> RecusarAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, motorDeCarreira.Recusar, cancelamento);

    /// <summary>O jogador pendura as luvas por vontade própria.</summary>
    public Task<JogadaDaCarreira> AposentarAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, (partida, carreira, _) => motorDeCarreira.Aposentar(partida, carreira), cancelamento);

    /// <summary>
    /// Entrega a carreira ao jogador automático, que a leva do ponto atual até a
    /// aposentadoria. É a saída de quem cansou no meio do caminho.
    /// </summary>
    public Task<JogadaDaCarreira> SimularORestoAsync(
        Guid partidaId,
        CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, motorDeCarreira.SimularOResto, cancelamento);

    /// <summary>Recupera a situação atual da carreira, sem alterar nada.</summary>
    public async Task<JogadaDaCarreira> ObterAsync(
        Guid partidaId,
        CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);

        return Montar(
            partida,
            PassoDaCarreira.Vazio,
            await CarregarRankingAsync(cancelamento),
            posicaoAnterior: null);
    }

    /// <summary>Junta a partida ao ranking da divisão em que ela está agora.</summary>
    private static JogadaDaCarreira Montar(
        Partida partida,
        PassoDaCarreira passo,
        RankingDoJogo ranking,
        int? posicaoAnterior) => new(
        partida,
        passo,
        ranking.Da(partida.ExigirCarreira().Estado.Categoria),
        posicaoAnterior);

    /// <summary>
    /// O esqueleto comum de toda jogada: carrega, exige que a carreira exista,
    /// aplica o movimento e salva.
    /// </summary>
    private async Task<JogadaDaCarreira> JogarAsync(
        Guid partidaId,
        Func<Partida, Carreira, RankingDoJogo, PassoDaCarreira> movimento,
        CancellationToken cancelamento)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var carreira = partida.ExigirCarreira();
        var ranking = await CarregarRankingAsync(cancelamento);

        var totalDeLutasAntesDaJogada = carreira.TotalDeLutas;
        var posicaoAntesDaJogada = carreira.Estado.PosicaoNoRanking;

        var passo = movimento(partida, carreira, ranking);

        // A carreira já está rastreada e a luta usa uma chave natural atribuída
        // (CarreiraId, Ordem). Sem declarar que a linha acabou de nascer, o EF
        // interpreta a chave preenchida como uma linha existente e tenta dar
        // UPDATE, causando uma falsa concorrência otimista.
        foreach (var lutaNova in carreira.Lutas.Skip(totalDeLutasAntesDaJogada))
        {
            contexto.Entry(lutaNova).State = EntityState.Added;
        }

        await contexto.SaveChangesAsync(cancelamento);

        return Montar(partida, passo, ranking, posicaoAntesDaJogada);
    }

    /// <summary>
    /// Carrega os ranqueados das oito divisões.
    /// </summary>
    /// <remarks>
    /// Vêm todas as divisões, e não só a do jogador, porque um campeão
    /// consolidado pode subir de peso no meio do passo — e aí os adversários
    /// da rodada seguinte precisam sair da divisão nova.
    /// <para>
    /// A leitura é sem rastreamento: estes atletas são referência de consulta,
    /// nunca são alterados pela carreira. O ranking oficial do acervo não muda
    /// quando o jogador sobe; o que muda é a posição dele, guardada na carreira.
    /// </para>
    /// </remarks>
    private async Task<RankingDoJogo> CarregarRankingAsync(CancellationToken cancelamento) =>
        new(await contexto.Lutadores
            .AsNoTracking()
            .Where(lutador => lutador.Categoria != null && lutador.PosicaoNoRanking != null)
            .ToListAsync(cancelamento));
}

/// <summary>
/// A partida depois da jogada, o que a jogada produziu e o ranking da divisão
/// em que ela aconteceu.
/// </summary>
/// <param name="Tabela">
/// O ranking real da divisão do jogador. A camada de contrato encaixa o jogador
/// nele na hora de responder.
/// </param>
/// <param name="PosicaoAnterior">
/// Onde o jogador estava antes desta jogada, para a tela poder animar a subida.
/// Nulo quando ele não estava ranqueado.
/// </param>
public sealed record JogadaDaCarreira(
    Partida Partida,
    PassoDaCarreira Passo,
    TabelaDaDivisao Tabela,
    int? PosicaoAnterior);
