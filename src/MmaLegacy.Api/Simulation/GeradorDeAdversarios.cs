using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Cria os adversários da carreira sob medida para o degrau em que o lutador
/// está.
/// </summary>
/// <remarks>
/// Os adversários são fictícios e descartáveis: não existem como entidade
/// persistida, só como linha do cartel. Isso mantém o acervo do draft — que é
/// de atletas reais — separado do elenco de figuração da simulação, e evita
/// dar a impressão de que o jogo está simulando lutas que aconteceram.
/// <para>
/// O estilo do adversário não é escolhido: ele é <b>deduzido</b> dos atributos
/// gerados, pela mesma regra que classifica o lutador do jogador. Uma fonte da
/// verdade só para o que é um nocauteador.
/// </para>
/// </remarks>
public sealed class GeradorDeAdversarios
{
    /// <summary>Nota de partida antes das ênfases do arquétipo e da calibragem.</summary>
    private const int NotaBase = 70;

    /// <summary>Quanto uma habilidade ganha por ser especialidade do arquétipo.</summary>
    private const int BonusDeEnfase = 10;

    /// <summary>Quanto uma habilidade perde por estar fora do arquétipo.</summary>
    private const int PenalidadeSemEnfase = -4;

    /// <summary>Variação aleatória por habilidade, para dois adversários do mesmo arquétipo não serem clones.</summary>
    private const int RuidoMaximo = 4;

    /// <summary>
    /// Faixa de overall esperada em cada degrau da carreira. É o que dá sentido
    /// à progressão: vencer no circuito regional não prova nada sobre aguentar
    /// uma disputa de cinturão.
    /// </summary>
    private static readonly Dictionary<EtapaDaCarreira, (int Minimo, int Maximo)> FaixasDeOverall = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = (60, 70),
        [EtapaDaCarreira.OrganizacaoNacional] = (68, 76),
        [EtapaDaCarreira.GrandeOrganizacao] = (74, 82),
        [EtapaDaCarreira.Top15] = (80, 87),
        [EtapaDaCarreira.Top5] = (85, 92),
        [EtapaDaCarreira.DisputaDeCinturao] = (90, 97),
        [EtapaDaCarreira.Campeao] = (88, 95)
    };

    /// <summary>
    /// Perfis de adversário. Cada um destaca um conjunto de habilidades; o
    /// estilo resultante sai da classificação, não daqui.
    /// </summary>
    private static readonly IReadOnlyList<Habilidade[]> Arquetipos =
    [
        [Habilidade.Striking, Habilidade.Potencia, Habilidade.Velocidade],
        [Habilidade.Wrestling, Habilidade.Cardio, Habilidade.Resistencia],
        [Habilidade.JiuJitsu, Habilidade.Wrestling],
        [Habilidade.InteligenciaDeLuta, Habilidade.Striking, Habilidade.Resistencia],
        [Habilidade.Velocidade, Habilidade.Striking, Habilidade.Cardio],
        []
    ];

    private static readonly IReadOnlyList<string> Prenomes =
    [
        "Adriano", "Bruno", "Caio", "Dmitri", "Eduardo", "Fabio", "Gustavo", "Hugo",
        "Igor", "Jonas", "Kaique", "Lucas", "Marcelo", "Nelson", "Otavio", "Pedro",
        "Rafael", "Samuel", "Tiago", "Vitor", "Andrei", "Cassio", "Diego", "Emerson",
        "Aleksei", "Malik", "Kenji", "Sione", "Tarek", "Yuri"
    ];

    private static readonly IReadOnlyList<string> Sobrenomes =
    [
        "Alencar", "Barreto", "Castilho", "Duarte", "Esteves", "Falcao", "Garcia",
        "Holanda", "Ibarra", "Junqueira", "Kovac", "Lomeu", "Machado", "Nogales",
        "Okafor", "Pontes", "Queiroz", "Rangel", "Serrano", "Tavares", "Urbano",
        "Valente", "Wojcik", "Ximenes", "Zanotti", "Almeida", "Bittar", "Cordeiro",
        "Vasquez", "Petrov"
    ];

    /// <summary>
    /// Sorteia um adversário compatível com o degrau informado.
    /// </summary>
    /// <param name="etapa">Degrau atual do lutador, que define a faixa de overall.</param>
    /// <param name="ajusteDeOverall">
    /// Correção aplicada à faixa. O motor de carreira usa isto para endurecer a
    /// divisão depois de uma mudança de categoria: subir de peso significa
    /// encarar gente maior no mesmo degrau.
    /// </param>
    /// <param name="sorteio">Fonte de aleatoriedade da partida.</param>
    public PerfilDeCombate Gerar(EtapaDaCarreira etapa, int ajusteDeOverall, Sorteio sorteio)
    {
        ArgumentNullException.ThrowIfNull(sorteio);

        var faixa = FaixasDeOverall[etapa];
        var overallAlvo = sorteio.Inteiro(faixa.Minimo, faixa.Maximo + 1) + ajusteDeOverall;
        var enfases = sorteio.Escolher(Arquetipos).ToHashSet();

        var atributos = GerarAtributosCalibrados(overallAlvo, enfases, sorteio);
        return PerfilDeCombate.Montar(GerarNome(sorteio), atributos);
    }

    /// <summary>
    /// Monta um rascunho a partir do arquétipo e depois reescala todas as notas
    /// para o overall bater com o alvo.
    /// </summary>
    /// <remarks>
    /// A reescala funciona porque o overall é uma combinação linear das notas:
    /// multiplicar todas por um fator multiplica o overall pelo mesmo fator. O
    /// resultado fica a menos de um ponto do alvo, o que é folga suficiente —
    /// e preserva o formato do arquétipo, que é o que importa para o matchup.
    /// </remarks>
    private static Atributos GerarAtributosCalibrados(
        int overallAlvo,
        IReadOnlySet<Habilidade> enfases,
        Sorteio sorteio)
    {
        var rascunho = Habilidades.Todas.ToDictionary(
            habilidade => habilidade,
            habilidade => Atributos.LimitarNaEscala(
                NotaBase
                + (enfases.Contains(habilidade) ? BonusDeEnfase : PenalidadeSemEnfase)
                + sorteio.Inteiro(-RuidoMaximo, RuidoMaximo + 1)));

        var fator = overallAlvo / CalculadoraDeOverall.Calcular(Atributos.APartirDe(rascunho));

        var calibrado = rascunho.ToDictionary(
            par => par.Key,
            par => Atributos.LimitarNaEscala((int)Math.Round(par.Value * fator, MidpointRounding.AwayFromZero)));

        return Atributos.APartirDe(calibrado);
    }

    private static string GerarNome(Sorteio sorteio) =>
        $"{sorteio.Escolher(Prenomes)} {sorteio.Escolher(Sobrenomes)}";
}
