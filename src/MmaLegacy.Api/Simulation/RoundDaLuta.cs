using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// O que aconteceu em um round, do ponto de vista do lutador do jogador.
/// </summary>
/// <remarks>
/// O motor de luta sempre resolveu a luta round a round; ele só não contava a
/// ninguém. Este registro é essa conversa que faltava: com a carreira jogada,
/// o jogador acompanha a luta que ele escolheu se desenrolar, em vez de receber
/// só a sigla do resultado.
/// <para>
/// São fatos, não narração. A frase que aparece na tela é montada na camada de
/// contrato, para o motor não virar depositário de texto de interface.
/// </para>
/// </remarks>
/// <param name="Numero">Número do round, começando em 1.</param>
/// <param name="Vencedor">Quem levou o round nos cartões.</param>
/// <param name="LutadorBuscouQueda">O lutador do jogador tentou derrubar.</param>
/// <param name="LutadorControlou">E conseguiu, passando o round por cima.</param>
/// <param name="AdversarioBuscouQueda">O adversário tentou derrubar.</param>
/// <param name="AdversarioControlou">E conseguiu.</param>
/// <param name="FadigaDoLutador">Cansaço do lutador ao fim do round, de 0 a 100.</param>
/// <param name="FadigaDoAdversario">Cansaço do adversário ao fim do round, de 0 a 100.</param>
/// <param name="DanoDoLutador">Castigo acumulado pelo lutador, de 0 a 100.</param>
/// <param name="DanoDoAdversario">Castigo acumulado pelo adversário, de 0 a 100.</param>
/// <param name="Encerramento">
/// Como a luta acabou neste round, ou <c>null</c> se o round foi até o fim do
/// tempo. Quem venceu sai do resultado da luta, não daqui.
/// </param>
public sealed record RoundDaLuta(
    int Numero,
    VencedorDoRound Vencedor,
    bool LutadorBuscouQueda,
    bool LutadorControlou,
    bool AdversarioBuscouQueda,
    bool AdversarioControlou,
    int FadigaDoLutador,
    int FadigaDoAdversario,
    int DanoDoLutador,
    int DanoDoAdversario,
    MetodoDeEncerramento? Encerramento);
