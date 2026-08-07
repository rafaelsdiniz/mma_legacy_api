namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// O que aconteceu com a carreira depois de uma decisão do jogador.
/// </summary>
/// <remarks>
/// Existe para o front-end poder dar peso visual a cada coisa sem interpretar
/// texto. A frase que o jogador lê é montada na camada de contrato; aqui fica
/// apenas o tipo do acontecimento.
/// </remarks>
public enum EventoDaCarreira
{
    /// <summary>Subiu um degrau na escada.</summary>
    Promovido = 1,

    /// <summary>Caiu um degrau depois de ser dispensado.</summary>
    Rebaixado = 2,

    /// <summary>A organização rescindiu o contrato.</summary>
    Dispensado = 3,

    /// <summary>Ganhou o direito de disputar o cinturão.</summary>
    DisputaDeCinturaoMarcada = 4,

    /// <summary>Conquistou o cinturão.</summary>
    CinturaoConquistado = 5,

    /// <summary>Defendeu o cinturão com sucesso.</summary>
    CinturaoDefendido = 6,

    /// <summary>Perdeu o cinturão.</summary>
    CinturaoPerdido = 7,

    /// <summary>Mudou de categoria de peso atrás do segundo cinturão.</summary>
    MudouDeCategoria = 8,

    /// <summary>Fechou mais um ano de carreira: os atributos passaram pela curva de evolução.</summary>
    AnoVirado = 9,

    /// <summary>Recusou todas as ofertas da rodada e ficou parado.</summary>
    FicouInativo = 10,

    /// <summary>A carreira acabou.</summary>
    CarreiraEncerrada = 11,

    /// <summary>Saiu machucado da luta e vai ficar parado se tratando.</summary>
    Lesionou = 12,

    /// <summary>A lesão sarou e ele volta a receber ofertas.</summary>
    RecuperouDeLesao = 13
}
