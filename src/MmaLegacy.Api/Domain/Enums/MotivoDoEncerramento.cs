namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Por que a carreira acabou. É a última frase da história do lutador, e o
/// jogador merece lê-la: "aposentou aos 40" e "foi dispensado sem ninguém para
/// contratá-lo" são finais muito diferentes para o mesmo cartel.
/// </summary>
public enum MotivoDoEncerramento
{
    /// <summary>Chegou à idade em que ninguém mais compete.</summary>
    IdadeLimite = 1,

    /// <summary>Nocautes demais em um corpo velho demais.</summary>
    CorpoCastigado = 2,

    /// <summary>Veterano em queda livre: a sequência de derrotas encerrou o assunto.</summary>
    SequenciaDeDerrotas = 3,

    /// <summary>Passou dos 30 sem nunca ter engrenado.</summary>
    SemResultados = 4,

    /// <summary>
    /// Cortado no circuito regional, onde não existe degrau abaixo. É o fim mais
    /// cruel do jogo: a carreira não termina, ela simplesmente para de existir.
    /// </summary>
    SemContrato = 5,

    /// <summary>Bateu o teto de lutas que um corpo aguenta.</summary>
    LimiteDeLutas = 6,

    /// <summary>O jogador pendurou as luvas por vontade própria.</summary>
    EscolhaDoLutador = 7
}
