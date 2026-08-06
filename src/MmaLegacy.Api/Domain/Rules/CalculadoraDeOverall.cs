using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// Converte os oito atributos em uma nota geral.
/// </summary>
/// <remarks>
/// Não é média simples: cada habilidade pesa conforme o quanto decide lutas.
/// Striking e wrestling pesam mais porque definem onde a luta acontece;
/// velocidade e resistência pesam menos porque são qualidades de apoio.
/// <para>
/// O overall serve para comparar lutadores e para calibrar adversários. Ele
/// <b>não</b> decide o resultado de uma luta sozinho — quem faz isso é o
/// <c>MotorDeLuta</c>, que também considera matchup, fadiga e idade.
/// </para>
/// </remarks>
public static class CalculadoraDeOverall
{
    /// <summary>Peso de cada habilidade na composição do overall. Sempre soma 1.</summary>
    public static readonly IReadOnlyDictionary<Habilidade, decimal> Pesos = new Dictionary<Habilidade, decimal>
    {
        [Habilidade.Striking] = 0.15m,
        [Habilidade.Potencia] = 0.12m,
        [Habilidade.Velocidade] = 0.10m,
        [Habilidade.Wrestling] = 0.15m,
        [Habilidade.JiuJitsu] = 0.13m,
        [Habilidade.Cardio] = 0.12m,
        [Habilidade.Resistencia] = 0.10m,
        [Habilidade.InteligenciaDeLuta] = 0.13m
    };

    /// <summary>
    /// Trava de balanceamento: se um ajuste de pesos deixar a soma diferente de
    /// 1, a aplicação quebra ao subir em vez de produzir overalls silenciosamente
    /// inflados ou deflacionados.
    /// </summary>
    static CalculadoraDeOverall()
    {
        var soma = Pesos.Values.Sum();
        if (soma != 1m)
        {
            throw new InvalidOperationException(
                $"Os pesos do overall devem somar exatamente 1, mas somam {soma}. " +
                "Reveja a tabela de pesos antes de continuar.");
        }
    }

    /// <summary>Overall do lutador, arredondado a uma casa decimal.</summary>
    public static decimal Calcular(Atributos atributos)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        var soma = Pesos.Sum(peso => atributos[peso.Key] * peso.Value);
        return Math.Round(soma, 1, MidpointRounding.AwayFromZero);
    }
}
