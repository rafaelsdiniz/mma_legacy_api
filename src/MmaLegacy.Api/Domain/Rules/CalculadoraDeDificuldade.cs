using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// Traduz a distância entre o jogador e o adversário no grau de dificuldade que
/// a tela mostra antes da decisão.
/// </summary>
/// <remarks>
/// Existe para o jogador não precisar fazer a conta de cabeça. Antes a oferta
/// dizia "overall 88" e cabia a ele lembrar que o próprio lutador estava em 82;
/// agora ela diz <see cref="GrauDeDificuldade.Dura"/>, que é a mesma informação
/// na forma em que a decisão é tomada.
/// <para>
/// A régua é a diferença de overall, e não a de atributos, pelo mesmo motivo
/// que o motor de luta ignora overall: overall é um resumo para humanos lerem.
/// Quem decide a luta continua sendo o confronto de estilos dentro do motor —
/// o grau é um aviso, não uma previsão.
/// </para>
/// </remarks>
public static class CalculadoraDeDificuldade
{
    /// <summary>Abaixo disto o adversário é nitidamente mais fraco.</summary>
    private const decimal LimiteDaTranquila = -3m;

    /// <summary>Até aqui os dois estão no mesmo nível.</summary>
    private const decimal LimiteDaEquilibrada = 1m;

    /// <summary>Acima disto a diferença deixa de ser de nível e vira de categoria.</summary>
    private const decimal LimiteDaDura = 6m;

    /// <summary>
    /// Calcula o grau de uma luta.
    /// </summary>
    /// <param name="overallDoAdversario">Overall de quem está do outro lado.</param>
    /// <param name="overallDoJogador">Overall do lutador do jogador hoje.</param>
    /// <param name="valendoCinturao">
    /// Luta de título sobe um grau. São cinco rounds contra o melhor da divisão:
    /// mesmo quando os números se parecem, o corpo sai diferente de lá.
    /// </param>
    public static GrauDeDificuldade Calcular(
        decimal overallDoAdversario,
        decimal overallDoJogador,
        bool valendoCinturao = false)
    {
        var diferenca = overallDoAdversario - overallDoJogador;

        var grau = diferenca switch
        {
            < LimiteDaTranquila => GrauDeDificuldade.Tranquila,
            <= LimiteDaEquilibrada => GrauDeDificuldade.Equilibrada,
            <= LimiteDaDura => GrauDeDificuldade.Dura,
            _ => GrauDeDificuldade.Brutal
        };

        return valendoCinturao ? Subir(grau) : grau;
    }

    private static GrauDeDificuldade Subir(GrauDeDificuldade grau) =>
        grau == GrauDeDificuldade.Brutal ? grau : grau + 1;
}
