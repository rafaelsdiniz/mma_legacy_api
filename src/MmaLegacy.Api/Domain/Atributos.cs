using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// As oito notas que descrevem um lutador. É um value object: imutável, sem
/// identidade própria e sempre válido — não existe instância com nota fora da
/// escala de <see cref="NotaMinima"/> a <see cref="NotaMaxima"/>.
/// </summary>
/// <remarks>
/// Toda mudança de atributo (evolução por idade, declínio, ajuste de categoria)
/// devolve uma instância nova via <see cref="ComAjustes"/>. Isso torna
/// impossível uma parte da simulação alterar por engano os atributos que outra
/// parte ainda vai ler no mesmo round.
/// </remarks>
public sealed class Atributos
{
    public const int NotaMinima = 1;
    public const int NotaMaxima = 100;

    public int Striking { get; private set; }
    public int Potencia { get; private set; }
    public int Velocidade { get; private set; }
    public int Wrestling { get; private set; }
    public int JiuJitsu { get; private set; }
    public int Cardio { get; private set; }
    public int Resistencia { get; private set; }
    public int InteligenciaDeLuta { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Atributos()
    {
    }

    public Atributos(
        int striking,
        int potencia,
        int velocidade,
        int wrestling,
        int jiuJitsu,
        int cardio,
        int resistencia,
        int inteligenciaDeLuta)
    {
        Striking = ValidarNota(striking, Habilidade.Striking);
        Potencia = ValidarNota(potencia, Habilidade.Potencia);
        Velocidade = ValidarNota(velocidade, Habilidade.Velocidade);
        Wrestling = ValidarNota(wrestling, Habilidade.Wrestling);
        JiuJitsu = ValidarNota(jiuJitsu, Habilidade.JiuJitsu);
        Cardio = ValidarNota(cardio, Habilidade.Cardio);
        Resistencia = ValidarNota(resistencia, Habilidade.Resistencia);
        InteligenciaDeLuta = ValidarNota(inteligenciaDeLuta, Habilidade.InteligenciaDeLuta);
    }

    /// <summary>Lê a nota de uma habilidade específica.</summary>
    public int this[Habilidade habilidade] => habilidade switch
    {
        Habilidade.Striking => Striking,
        Habilidade.Potencia => Potencia,
        Habilidade.Velocidade => Velocidade,
        Habilidade.Wrestling => Wrestling,
        Habilidade.JiuJitsu => JiuJitsu,
        Habilidade.Cardio => Cardio,
        Habilidade.Resistencia => Resistencia,
        Habilidade.InteligenciaDeLuta => InteligenciaDeLuta,
        _ => throw new DadoInvalidoException($"Habilidade desconhecida: {habilidade}.")
    };

    /// <summary>
    /// Monta os atributos a partir do mapa habilidade → nota produzido pelo
    /// draft. Exige as oito habilidades: um lutador pela metade não existe.
    /// </summary>
    public static Atributos APartirDe(IReadOnlyDictionary<Habilidade, int> notas)
    {
        ArgumentNullException.ThrowIfNull(notas);

        var faltantes = Habilidades.Todas.Where(habilidade => !notas.ContainsKey(habilidade)).ToList();
        if (faltantes.Count > 0)
        {
            var nomes = string.Join(", ", faltantes.Select(Habilidades.NomeDeExibicao));
            throw new DadoInvalidoException($"Faltam notas para as seguintes habilidades: {nomes}.");
        }

        return new Atributos(
            notas[Habilidade.Striking],
            notas[Habilidade.Potencia],
            notas[Habilidade.Velocidade],
            notas[Habilidade.Wrestling],
            notas[Habilidade.JiuJitsu],
            notas[Habilidade.Cardio],
            notas[Habilidade.Resistencia],
            notas[Habilidade.InteligenciaDeLuta]);
    }

    /// <summary>Pares habilidade → nota, úteis para iterar sem repetir os oito nomes.</summary>
    public IEnumerable<KeyValuePair<Habilidade, int>> Listar() =>
        Habilidades.Todas.Select(habilidade => new KeyValuePair<Habilidade, int>(habilidade, this[habilidade]));

    /// <summary>
    /// Devolve novos atributos com os deltas aplicados, cortando o resultado na
    /// escala válida. Habilidades ausentes do mapa ficam como estão.
    /// </summary>
    public Atributos ComAjustes(IReadOnlyDictionary<Habilidade, int> ajustes)
    {
        ArgumentNullException.ThrowIfNull(ajustes);

        var notas = Habilidades.Todas.ToDictionary(
            habilidade => habilidade,
            habilidade => LimitarNaEscala(this[habilidade] + ajustes.GetValueOrDefault(habilidade)));

        return APartirDe(notas);
    }

    /// <summary>A habilidade de maior nota — a "maior qualidade" do lutador.</summary>
    public Habilidade MaiorHabilidade() => Listar().MaxBy(par => par.Value).Key;

    /// <summary>A habilidade de menor nota — a "principal fraqueza" do lutador.</summary>
    public Habilidade MenorHabilidade() => Listar().MinBy(par => par.Value).Key;

    /// <summary>
    /// Distância entre a maior e a menor nota. Quanto menor, mais equilibrado é
    /// o lutador — é o que separa um "lutador completo" de um especialista.
    /// </summary>
    public int Amplitude() => Listar().Max(par => par.Value) - Listar().Min(par => par.Value);

    /// <summary>Corta um valor qualquer para dentro da escala de notas.</summary>
    public static int LimitarNaEscala(int nota) => Math.Clamp(nota, NotaMinima, NotaMaxima);

    private static int ValidarNota(int nota, Habilidade habilidade)
    {
        if (nota < NotaMinima || nota > NotaMaxima)
        {
            throw new DadoInvalidoException(
                $"A nota de {Habilidades.NomeDeExibicao(habilidade)} deve estar entre " +
                $"{NotaMinima} e {NotaMaxima}, mas recebeu {nota}.");
        }

        return nota;
    }
}
