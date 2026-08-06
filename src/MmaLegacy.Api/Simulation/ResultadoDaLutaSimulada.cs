using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// O que o motor de luta devolve, sempre da perspectiva do primeiro perfil
/// passado para a simulação — o lutador do jogador.
/// </summary>
/// <param name="Resultado">Vitória, derrota ou empate.</param>
/// <param name="Metodo">Como a luta terminou.</param>
/// <param name="RoundDoEncerramento">
/// Round em que acabou. Nas decisões, é igual ao número de rounds programados.
/// </param>
public sealed record ResultadoDaLutaSimulada(
    ResultadoDaLuta Resultado,
    MetodoDeEncerramento Metodo,
    int RoundDoEncerramento);
