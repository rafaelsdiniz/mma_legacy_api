using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Data;

/// <summary>
/// Deriva as notas de um atleta ranqueado a partir da posição dele e de um
/// arquétipo de estilo.
/// </summary>
/// <remarks>
/// Existe por honestidade. Para Alex Pereira, Islam Makhachev e uma dúzia de
/// outros dá para atribuir notas com fundamento; para o décimo terceiro do
/// peso-galo, não. Inventar oito números com cara de informado seria pior do
/// que assumir o método — e as notas do jogo são declaradamente estimativas
/// editoriais para equilibrar o draft, não avaliação do atleta real.
/// <para>
/// O que a posição no ranking carrega de verdade é <b>nível competitivo</b>, e
/// é só isso que é usado aqui: o campeão parte de um patamar, o décimo quinto
/// de outro. O arquétipo distribui esse total entre as oito habilidades.
/// </para>
/// <para>
/// Nada é aleatório: a variação por atleta vem de um hash do nome, então o
/// mesmo lutador produz sempre as mesmas notas em qualquer máquina e em
/// qualquer execução do seed.
/// </para>
/// </remarks>
public static class NotasPorRanking
{
    /// <summary>Overall do campeão da divisão.</summary>
    private const decimal OverallDoCampeao = 90m;

    /// <summary>Quanto o overall cai a cada posição abaixo do cinturão.</summary>
    private const decimal QuedaPorPosicao = 0.8m;

    private const int NotaBase = 74;
    private const int BonusDeEnfase = 11;
    private const int PenalidadeSemEnfase = -5;

    /// <summary>Amplitude da variação por atleta, para dois do mesmo arquétipo não serem clones.</summary>
    private const int VariacaoMaxima = 4;

    /// <summary>Perfis de estilo. O estilo final é deduzido das notas, não declarado aqui.</summary>
    public enum Arquetipo
    {
        /// <summary>Vive da luta em pé: striking, potência e velocidade.</summary>
        Striker,

        /// <summary>Pressão, quedas e ritmo: wrestling, cardio e resistência.</summary>
        Wrestler,

        /// <summary>Chão e finalização: jiu-jítsu e wrestling.</summary>
        Grappler,

        /// <summary>Leitura e paciência: fight IQ, striking e resistência.</summary>
        Tecnico,

        /// <summary>Sem especialidade marcante e sem buraco grande.</summary>
        Completo
    }

    private static readonly Dictionary<Arquetipo, Habilidade[]> Enfases = new()
    {
        [Arquetipo.Striker] = [Habilidade.Striking, Habilidade.Potencia, Habilidade.Velocidade],
        [Arquetipo.Wrestler] = [Habilidade.Wrestling, Habilidade.Cardio, Habilidade.Resistencia],
        [Arquetipo.Grappler] = [Habilidade.JiuJitsu, Habilidade.Wrestling],
        [Arquetipo.Tecnico] =
            [Habilidade.InteligenciaDeLuta, Habilidade.Striking, Habilidade.Resistencia],
        [Arquetipo.Completo] = []
    };

    /// <summary>
    /// Monta os atributos de um atleta na posição informada.
    /// </summary>
    /// <param name="nome">Usado como semente da variação individual.</param>
    /// <param name="posicaoNoRanking">0 para o campeão, 1 a 15 para os ranqueados.</param>
    /// <param name="arquetipo">Como o total é distribuído entre as habilidades.</param>
    public static Atributos Derivar(string nome, int posicaoNoRanking, Arquetipo arquetipo)
    {
        var enfases = Enfases[arquetipo].ToHashSet();
        var overallAlvo = OverallDoCampeao - (posicaoNoRanking * QuedaPorPosicao);

        var rascunho = Habilidades.Todas.ToDictionary(
            habilidade => habilidade,
            habilidade => Atributos.LimitarNaEscala(
                NotaBase
                + (enfases.Contains(habilidade) ? BonusDeEnfase : PenalidadeSemEnfase)
                + Variacao(nome, habilidade)));

        // A reescala funciona porque o overall é combinação linear das notas:
        // multiplicar todas por um fator multiplica o overall pelo mesmo fator.
        var fator = overallAlvo / CalculadoraDeOverall.Calcular(Atributos.APartirDe(rascunho));

        return Atributos.APartirDe(rascunho.ToDictionary(
            par => par.Key,
            par => Atributos.LimitarNaEscala(
                (int)Math.Round(par.Value * fator, MidpointRounding.AwayFromZero))));
    }

    /// <summary>
    /// Variação determinística por atleta e habilidade.
    /// </summary>
    /// <remarks>
    /// Deriva de um hash FNV-1a do nome combinado com a habilidade. Não usa
    /// <c>string.GetHashCode</c> de propósito: ele é aleatorizado por processo
    /// no .NET, o que faria as notas do acervo mudarem a cada reinício da API.
    /// </remarks>
    private static int Variacao(string nome, Habilidade habilidade)
    {
        const uint DeslocamentoInicial = 2166136261;
        const uint Primo = 16777619;

        var hash = DeslocamentoInicial;
        foreach (var caractere in $"{nome}:{habilidade}")
        {
            hash = (hash ^ caractere) * Primo;
        }

        // Mapeia para [-VariacaoMaxima, +VariacaoMaxima].
        return (int)(hash % ((VariacaoMaxima * 2) + 1)) - VariacaoMaxima;
    }
}
