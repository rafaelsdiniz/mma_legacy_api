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
    public async Task<Partida> EstrearAsync(Guid partidaId, CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);

        if (partida.Carreira is not null)
        {
            return partida;
        }

        var carreira = motorDeCarreira.Iniciar(partida);

        partida.EstrearCarreira(carreira);
        contexto.Carreiras.Add(carreira);
        await contexto.SaveChangesAsync(cancelamento);

        return partida;
    }

    /// <summary>O jogador aceita uma das ofertas na mesa e a luta acontece.</summary>
    /// <param name="indiceDaOferta">Índice da oferta escolhida, começando em 1.</param>
    public Task<JogadaDaCarreira> AceitarAsync(
        Guid partidaId,
        int indiceDaOferta,
        CancellationToken cancelamento = default) =>
        JogarAsync(
            partidaId,
            (partida, carreira) => motorDeCarreira.Aceitar(partida, carreira, indiceDaOferta),
            cancelamento);

    /// <summary>O jogador recusa a rodada inteira e fica parado.</summary>
    public Task<JogadaDaCarreira> RecusarAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, motorDeCarreira.Recusar, cancelamento);

    /// <summary>O jogador pendura as luvas por vontade própria.</summary>
    public Task<JogadaDaCarreira> AposentarAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, motorDeCarreira.Aposentar, cancelamento);

    /// <summary>
    /// Entrega a carreira ao jogador automático, que a leva do ponto atual até a
    /// aposentadoria. É a saída de quem cansou no meio do caminho.
    /// </summary>
    public Task<JogadaDaCarreira> SimularORestoAsync(
        Guid partidaId,
        CancellationToken cancelamento = default) =>
        JogarAsync(partidaId, motorDeCarreira.SimularOResto, cancelamento);

    /// <summary>Recupera a partida com a carreira, sem alterar nada.</summary>
    public Task<Partida> ObterAsync(Guid partidaId, CancellationToken cancelamento = default) =>
        servicoDePartida.ObterAsync(partidaId, cancelamento);

    /// <summary>
    /// O esqueleto comum de toda jogada: carrega, exige que a carreira exista,
    /// aplica o movimento e salva.
    /// </summary>
    private async Task<JogadaDaCarreira> JogarAsync(
        Guid partidaId,
        Func<Partida, Carreira, PassoDaCarreira> movimento,
        CancellationToken cancelamento)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        var carreira = partida.ExigirCarreira();
        var totalDeLutasAntesDaJogada = carreira.TotalDeLutas;

        var passo = movimento(partida, carreira);

        // A carreira já está rastreada e a luta usa uma chave natural atribuída
        // (CarreiraId, Ordem). Sem declarar que a linha acabou de nascer, o EF
        // interpreta a chave preenchida como uma linha existente e tenta dar
        // UPDATE, causando uma falsa concorrência otimista.
        foreach (var lutaNova in carreira.Lutas.Skip(totalDeLutasAntesDaJogada))
        {
            contexto.Entry(lutaNova).State = EntityState.Added;
        }

        await contexto.SaveChangesAsync(cancelamento);

        return new JogadaDaCarreira(partida, passo);
    }
}

/// <summary>A partida depois da jogada e o que a jogada produziu.</summary>
public sealed record JogadaDaCarreira(Partida Partida, PassoDaCarreira Passo);
