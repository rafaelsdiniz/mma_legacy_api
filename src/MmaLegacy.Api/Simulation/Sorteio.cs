namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Única fonte de aleatoriedade da simulação, presa a uma semente.
/// </summary>
/// <remarks>
/// Toda a reprodutibilidade do jogo depende de nada mais sortear nada por
/// fora: se um trecho do motor chamar <c>Random.Shared</c> ou
/// <c>DateTime.Now</c>, a mesma semente deixa de produzir a mesma carreira e
/// o draft diário perde o sentido. Por isso o motor recebe um
/// <see cref="Sorteio"/> por parâmetro em vez de criar o seu.
/// <para>
/// A sequência é estável para uma mesma semente dentro de uma mesma versão do
/// .NET. Atualizações maiores do runtime podem mudar o algoritmo interno de
/// <see cref="Random"/> — partidas já salvas continuam válidas porque o
/// resultado fica persistido, mas uma re-simulação poderia divergir.
/// </para>
/// </remarks>
public sealed class Sorteio
{
    /// <summary>Limites do fator de sorte aplicado lance a lance.</summary>
    private const double FatorDeSorteMinimo = 0.90;
    private const double FatorDeSorteMaximo = 1.10;

    private readonly Random _random;

    public Sorteio(int semente)
    {
        Semente = semente;
        _random = new Random(semente);
    }

    /// <summary>A semente que originou esta sequência.</summary>
    public int Semente { get; }

    /// <summary>Inteiro no intervalo [minimo, maximoExclusivo).</summary>
    public int Inteiro(int minimo, int maximoExclusivo) => _random.Next(minimo, maximoExclusivo);

    /// <summary>Fração no intervalo [0, 1).</summary>
    public double Fracao() => _random.NextDouble();

    /// <summary>Número real no intervalo [minimo, maximo).</summary>
    public double Entre(double minimo, double maximo) => minimo + (_random.NextDouble() * (maximo - minimo));

    /// <summary>Verdadeiro com a probabilidade informada, de 0 a 1.</summary>
    public bool Acontece(double probabilidade) => _random.NextDouble() < probabilidade;

    /// <summary>
    /// O fator aleatório controlado do jogo: multiplica um valor por algo entre
    /// 0,90 e 1,10. Estreito de propósito — largo o bastante para criar zebras,
    /// apertado o bastante para os atributos ainda decidirem a maioria das lutas.
    /// </summary>
    public double FatorDeSorte() => Entre(FatorDeSorteMinimo, FatorDeSorteMaximo);

    /// <summary>Um item qualquer da lista.</summary>
    public T Escolher<T>(IReadOnlyList<T> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);

        if (itens.Count == 0)
        {
            throw new ArgumentException("Não é possível sortear de uma lista vazia.", nameof(itens));
        }

        return itens[_random.Next(itens.Count)];
    }

    /// <summary>
    /// Embaralha por Fisher-Yates, sem alterar a coleção de origem. É como os
    /// oito atletas do draft são sorteados e postos em ordem.
    /// </summary>
    public List<T> Embaralhar<T>(IEnumerable<T> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);

        var embaralhados = itens.ToList();
        for (var atual = embaralhados.Count - 1; atual > 0; atual--)
        {
            var trocarCom = _random.Next(atual + 1);
            (embaralhados[atual], embaralhados[trocarCom]) = (embaralhados[trocarCom], embaralhados[atual]);
        }

        return embaralhados;
    }
}
