using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Simulation;

namespace MmaLegacy.Api.Services;

/// <summary>
/// Dispara a simulação de carreira de uma partida com o draft concluído.
/// </summary>
public sealed class ServicoDeCarreira(
    ContextoDoJogo contexto,
    ServicoDePartida servicoDePartida,
    MotorDeCarreira motorDeCarreira)
{
    /// <summary>
    /// Simula a carreira e a persiste.
    /// </summary>
    /// <remarks>
    /// A simulação roda uma vez só por partida. Pedir de novo devolve a mesma
    /// carreira, sem re-simular: com a semente fixa o resultado seria idêntico,
    /// e recusar a segunda chamada explicitamente evita que uma tela recarregada
    /// pareça estar sorteando outra vida para o mesmo lutador.
    /// </remarks>
    public async Task<Carreira> SimularAsync(Guid partidaId, CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);

        if (partida.Carreira is { } jaSimulada)
        {
            return jaSimulada;
        }

        var carreira = motorDeCarreira.Simular(partida);

        partida.RegistrarCarreira(carreira);
        contexto.Carreiras.Add(carreira);
        await contexto.SaveChangesAsync(cancelamento);

        return carreira;
    }

    /// <summary>Recupera a carreira já simulada de uma partida.</summary>
    public async Task<Carreira> ObterAsync(Guid partidaId, CancellationToken cancelamento = default)
    {
        var partida = await servicoDePartida.ObterAsync(partidaId, cancelamento);
        return partida.ExigirCarreiraSimulada();
    }
}
