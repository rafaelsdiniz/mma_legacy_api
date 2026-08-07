using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// A chance de o lutador não sair inteiro da luta que aceitou.
/// </summary>
/// <remarks>
/// O número que esta classe calcula é o mesmo que a tela mostra na oferta e o
/// mesmo que o sorteio usa depois. Não existe risco escondido: se o jogador leu
/// 14%, o dado que rolou tinha 14%. Um jogo que anuncia uma probabilidade e
/// aplica outra ensina o jogador a ignorar o que está escrito.
/// <para>
/// O que o resultado da luta muda não é <b>se</b> a lesão acontece, e sim
/// <b>qual</b> ela é. Essa parte é sorteio, e por isso mora no
/// <c>SorteioDeLesao</c>, do lado da simulação: aqui fica só a regra, que é
/// pura e o jogador precisa poder ler antes de decidir.
/// </para>
/// </remarks>
public static class CalculadoraDeLesao
{
    /// <summary>Risco base de cada grau, para um lutador jovem e de resistência média.</summary>
    private static readonly Dictionary<GrauDeDificuldade, double> RiscoBase = new()
    {
        [GrauDeDificuldade.Tranquila] = 0.02,
        [GrauDeDificuldade.Equilibrada] = 0.05,
        [GrauDeDificuldade.Dura] = 0.11,
        [GrauDeDificuldade.Brutal] = 0.20
    };

    /// <summary>Idade a partir da qual o corpo começa a cobrar juros.</summary>
    private const int IdadeEmQueOCorpoCobra = 30;

    /// <summary>Quanto cada ano depois dos 30 acrescenta ao risco.</summary>
    private const double AcrescimoPorAnoDeIdade = 0.06;

    /// <summary>Teto do risco: nenhuma luta é uma sentença.</summary>
    public const double RiscoMaximo = 0.45;

    /// <summary>Resistência considerada média ao calibrar o risco.</summary>
    private const double ResistenciaDeReferencia = 80.0;

    /// <summary>Quanto a resistência mexe no risco, para cima e para baixo.</summary>
    private const double PesoDaResistencia = 0.6;

    /// <summary>
    /// A chance de o lutador se machucar nesta luta, de 0 a 1.
    /// </summary>
    /// <param name="grau">Quão dura é a luta para ele.</param>
    /// <param name="idade">Idade do lutador hoje.</param>
    /// <param name="resistencia">Nota de resistência atual, que é o que segura o corpo.</param>
    public static double Risco(GrauDeDificuldade grau, int idade, int resistencia)
    {
        var porIdade = 1 + (Math.Max(0, idade - IdadeEmQueOCorpoCobra) * AcrescimoPorAnoDeIdade);
        var porResistencia = 1 + ((ResistenciaDeReferencia - resistencia) / 100.0 * PesoDaResistencia);

        return Math.Clamp(RiscoBase[grau] * porIdade * porResistencia, 0, RiscoMaximo);
    }
}
