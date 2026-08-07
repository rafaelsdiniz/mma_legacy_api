using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Sorteia o que o lutador machucou e o tamanho do estrago, depois de a
/// <see cref="Domain.Rules.CalculadoraDeLesao"/> já ter decidido que ele se
/// machucou.
/// </summary>
/// <remarks>
/// A separação é proposital: a chance é regra que o jogador lê antes de
/// aceitar, o tipo e a gravidade são consequência que ele descobre depois. Uma
/// coisa precisa ser previsível; a outra, não.
/// </remarks>
public static class SorteioDeLesao
{
    /// <summary>
    /// Monta a lesão de quem acabou de lutar.
    /// </summary>
    /// <param name="grau">Quão dura foi a luta, que pesa na gravidade.</param>
    /// <param name="perdeuPorNocaute">
    /// Quem apagou machucou a cabeça. Não é sorteio: é a consequência direta do
    /// que acabou de acontecer com ele.
    /// </param>
    /// <param name="roundsDisputados">
    /// Lutas longas quebram mão e costela; lutas curtas quebram joelho e abrem
    /// corte. É o desgaste da troca contra o acidente do primeiro round.
    /// </param>
    /// <param name="idade">Idade do lutador no dia da luta.</param>
    public static Lesao Montar(
        GrauDeDificuldade grau,
        bool perdeuPorNocaute,
        int roundsDisputados,
        int idade,
        Sorteio sorteio)
    {
        ArgumentNullException.ThrowIfNull(sorteio);

        return new Lesao(
            SortearTipo(perdeuPorNocaute, roundsDisputados, sorteio),
            SortearGravidade(grau, sorteio),
            idade);
    }

    private static TipoDeLesao SortearTipo(bool perdeuPorNocaute, int roundsDisputados, Sorteio sorteio)
    {
        if (perdeuPorNocaute)
        {
            return TipoDeLesao.Concussao;
        }

        TipoDeLesao[] possiveis = roundsDisputados >= 3
            ? [TipoDeLesao.MaoFraturada, TipoDeLesao.CostelaTrincada, TipoDeLesao.Corte]
            : [TipoDeLesao.JoelhoLesionado, TipoDeLesao.Corte, TipoDeLesao.MaoFraturada];

        return sorteio.Escolher(possiveis);
    }

    /// <summary>
    /// Sorteia o tamanho do estrago. Quanto mais dura a luta, mais peso nas
    /// gravidades altas — mas uma luta tranquila também pode terminar com o
    /// joelho destruído, porque é assim que acontece.
    /// </summary>
    private static GravidadeDaLesao SortearGravidade(GrauDeDificuldade grau, Sorteio sorteio)
    {
        var chanceDeGrave = grau switch
        {
            GrauDeDificuldade.Tranquila => 0.08,
            GrauDeDificuldade.Equilibrada => 0.14,
            GrauDeDificuldade.Dura => 0.24,
            _ => 0.35
        };

        if (sorteio.Acontece(chanceDeGrave))
        {
            return GravidadeDaLesao.Grave;
        }

        return sorteio.Acontece(0.45) ? GravidadeDaLesao.Moderada : GravidadeDaLesao.Leve;
    }
}
