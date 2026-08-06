using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// As regras da escada: quanto se luta por ano em cada degrau, quantas vitórias
/// promovem, quantas ofertas a organização põe na mesa e a partir de quantos
/// tropeços ela rescinde o contrato.
/// </summary>
/// <remarks>
/// Ficam aqui, e não escondidas dentro do motor, porque o jogador precisa
/// vê-las. "Faltam 2 vitórias para o top 15" e "mais uma derrota e você está
/// fora" são a tensão do jogo — se o motor guardasse esses números para si, a
/// tela teria de repeti-los, e um dia os dois discordariam.
/// </remarks>
public static class RegrasDaCarreira
{
    /// <summary>Derrotas seguidas que fazem a organização rescindir o contrato.</summary>
    public const int DerrotasParaSerDispensado = 3;

    /// <summary>
    /// Rodadas seguidas recusadas que fazem a organização desistir do lutador.
    /// Quem não luta não vende ingresso.
    /// </summary>
    public const int RecusasParaSerDispensado = 3;

    /// <summary>Quantos compromissos cabem em um ano. No topo se luta menos.</summary>
    private static readonly Dictionary<EtapaDaCarreira, int> Calendario = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = 4,
        [EtapaDaCarreira.OrganizacaoNacional] = 3,
        [EtapaDaCarreira.GrandeOrganizacao] = 2,
        [EtapaDaCarreira.Top15] = 2,
        [EtapaDaCarreira.Top5] = 2,
        [EtapaDaCarreira.DisputaDeCinturao] = 2,
        [EtapaDaCarreira.Campeao] = 2
    };

    /// <summary>Vitórias no degrau necessárias para subir ao próximo.</summary>
    private static readonly Dictionary<EtapaDaCarreira, int> Promocao = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = 4,
        [EtapaDaCarreira.OrganizacaoNacional] = 4,
        [EtapaDaCarreira.GrandeOrganizacao] = 3,
        [EtapaDaCarreira.Top15] = 3,
        [EtapaDaCarreira.Top5] = 2
    };

    /// <summary>
    /// Quantas lutas a organização oferece por rodada.
    /// </summary>
    /// <remarks>
    /// Estreante não escolhe: no circuito regional e na organização nacional é
    /// pegar ou largar, porque ninguém sabe quem ele é. Poder de escolha é
    /// consequência de nome, e nome se constrói ganhando — por isso o leque só
    /// abre da grande organização para cima, e fecha de novo nas lutas de
    /// título, onde existe um campeão e existe um desafiante.
    /// </remarks>
    private static readonly Dictionary<EtapaDaCarreira, int> Ofertas = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = 1,
        [EtapaDaCarreira.OrganizacaoNacional] = 1,
        [EtapaDaCarreira.GrandeOrganizacao] = 2,
        [EtapaDaCarreira.Top15] = 2,
        [EtapaDaCarreira.Top5] = 3,
        [EtapaDaCarreira.DisputaDeCinturao] = 1,
        [EtapaDaCarreira.Campeao] = 1
    };

    public static int CompromissosPorTemporada(EtapaDaCarreira etapa) => Calendario[etapa];

    /// <summary>
    /// Vitórias que promovem ao degrau seguinte, ou zero nos degraus de título,
    /// de onde não se sobe — se defende ou se perde.
    /// </summary>
    public static int VitoriasParaSubir(EtapaDaCarreira etapa) => Promocao.GetValueOrDefault(etapa);

    public static int OfertasNaMesa(EtapaDaCarreira etapa) => Ofertas[etapa];

    public static NivelDaOrganizacao OrganizacaoDe(EtapaDaCarreira etapa) => etapa switch
    {
        EtapaDaCarreira.CircuitoRegional => NivelDaOrganizacao.CircuitoRegional,
        EtapaDaCarreira.OrganizacaoNacional => NivelDaOrganizacao.OrganizacaoNacional,
        _ => NivelDaOrganizacao.GrandeOrganizacao
    };
}
