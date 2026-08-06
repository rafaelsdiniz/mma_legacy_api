using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Envelhece o lutador de um ano para o outro.
/// </summary>
/// <remarks>
/// Os atributos escolhidos no draft são o ponto de partida, não o destino. A
/// curva separa o que a idade dá do que a idade tira: técnica e leitura de
/// jogo sobem quase a carreira inteira, enquanto velocidade, potência e cardio
/// têm prazo. É por isso que um lutador montado com notas físicas altas
/// costuma explodir cedo e apagar rápido, e um montado em técnica e fight IQ
/// tem carreiras mais longas — a escolha no draft ecoa vinte anos depois.
/// </remarks>
public static class CurvaDeEvolucao
{
    /// <summary>Técnica pura: cresce com treino e só cai bem no fim.</summary>
    private static readonly Habilidade[] Tecnicos =
        [Habilidade.Striking, Habilidade.Wrestling, Habilidade.JiuJitsu];

    /// <summary>Qualidades explosivas, as primeiras a ir embora.</summary>
    private static readonly Habilidade[] Explosivos =
        [Habilidade.Velocidade, Habilidade.Potencia];

    /// <summary>
    /// Faixas etárias do README, com o intervalo de variação anual de cada
    /// grupo de atributos. Os valores são intervalos, não números fixos: dois
    /// lutadores idênticos envelhecem diferente.
    /// </summary>
    private static readonly FaixaDeEvolucao[] Faixas =
    [
        //                  idade    técnicos      explosivos    cardio        resistência   IQ
        new(18, 22, new(2, 4), new(1, 2), new(1, 2), new(0, 1), 1),
        new(23, 27, new(1, 3), new(0, 1), new(1, 2), new(0, 1), 1),
        new(28, 32, new(0, 1), new(-1, 0), new(0, 1), new(-1, 0), 1),
        new(33, 35, new(0, 1), new(-2, -1), new(-1, 0), new(-1, 0), 1),
        new(36, int.MaxValue, new(-1, 0), new(-3, -1), new(-2, -1), new(-2, -1), 1)
    ];

    /// <summary>Resistência perdida a cada nocaute sofrido no ano.</summary>
    private const int DesgastePorNocauteSofrido = 1;

    /// <summary>
    /// Quanto uma habilidade pode crescer, no máximo, acima da nota com que o
    /// lutador estreou.
    /// </summary>
    /// <remarks>
    /// É o teto de potencial, e é o que mantém o draft relevante do começo ao
    /// fim. Sem ele, uma década de ganhos anuais empurraria qualquer lutador
    /// para perto de 100 e as escolhas do jogador virariam detalhe: bastaria
    /// sobreviver. Com ele, montar um lutador nota 96 é diferente de montar um
    /// nota 80, e continua sendo diferente aos 32 anos.
    /// <para>
    /// O teto vale só para ganhos. Perdas por idade e por nocaute sempre se
    /// aplicam — declínio não tem piso além da escala de notas.
    /// </para>
    /// </remarks>
    public const int TetoDeEvolucaoPorHabilidade = 8;

    /// <summary>
    /// Aplica um ano de carreira aos atributos.
    /// </summary>
    /// <param name="atributos">Atributos no início do ano.</param>
    /// <param name="atributosDeEstreia">
    /// Atributos com que o lutador saiu do draft. Definem o teto de potencial:
    /// nenhuma habilidade cresce mais de <see cref="TetoDeEvolucaoPorHabilidade"/>
    /// pontos acima do que era na estreia.
    /// </param>
    /// <param name="idade">Idade que o lutador tinha durante o ano.</param>
    /// <param name="nocautesSofridosNoAno">
    /// Nocautes sofridos no período. Cobram resistência à parte da idade — são
    /// as guerras que encurtam carreira.
    /// </param>
    /// <param name="sorteio">Fonte de aleatoriedade da partida.</param>
    public static Atributos AplicarAno(
        Atributos atributos,
        Atributos atributosDeEstreia,
        int idade,
        int nocautesSofridosNoAno,
        Sorteio sorteio)
    {
        ArgumentNullException.ThrowIfNull(atributos);
        ArgumentNullException.ThrowIfNull(atributosDeEstreia);
        ArgumentNullException.ThrowIfNull(sorteio);

        var faixa = LocalizarFaixa(idade);
        var ajustes = new Dictionary<Habilidade, int>();

        foreach (var habilidade in Tecnicos)
        {
            ajustes[habilidade] = faixa.Tecnicos.Sortear(sorteio);
        }

        foreach (var habilidade in Explosivos)
        {
            ajustes[habilidade] = faixa.Explosivos.Sortear(sorteio);
        }

        ajustes[Habilidade.Cardio] = faixa.Cardio.Sortear(sorteio);
        ajustes[Habilidade.InteligenciaDeLuta] = faixa.GanhoDeInteligencia;
        ajustes[Habilidade.Resistencia] =
            faixa.Resistencia.Sortear(sorteio) - (nocautesSofridosNoAno * DesgastePorNocauteSofrido);

        return atributos.ComAjustes(LimitarGanhosAoPotencial(atributos, atributosDeEstreia, ajustes));
    }

    /// <summary>
    /// Corta os ganhos que ultrapassariam o teto de potencial da habilidade,
    /// deixando as perdas intactas.
    /// </summary>
    private static Dictionary<Habilidade, int> LimitarGanhosAoPotencial(
        Atributos atuais,
        Atributos deEstreia,
        Dictionary<Habilidade, int> ajustes)
    {
        foreach (var habilidade in ajustes.Keys.ToList())
        {
            var ganho = ajustes[habilidade];
            if (ganho <= 0)
            {
                continue;
            }

            var teto = deEstreia[habilidade] + TetoDeEvolucaoPorHabilidade;
            var folga = teto - atuais[habilidade];
            ajustes[habilidade] = Math.Max(0, Math.Min(ganho, folga));
        }

        return ajustes;
    }

    private static FaixaDeEvolucao LocalizarFaixa(int idade) =>
        Faixas.FirstOrDefault(faixa => idade >= faixa.IdadeMinima && idade <= faixa.IdadeMaxima)
        ?? Faixas[^1];

    /// <summary>Variação anual possível de um grupo de atributos, em pontos.</summary>
    private sealed record Variacao(int Minimo, int Maximo)
    {
        public int Sortear(Sorteio sorteio) => sorteio.Inteiro(Minimo, Maximo + 1);
    }

    /// <summary>Como um lutador evolui dentro de uma faixa etária.</summary>
    private sealed record FaixaDeEvolucao(
        int IdadeMinima,
        int IdadeMaxima,
        Variacao Tecnicos,
        Variacao Explosivos,
        Variacao Cardio,
        Variacao Resistencia,
        int GanhoDeInteligencia);
}
