using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// O treino que antecede a luta: onde o jogador escolhe em que o lutador vai
/// melhorar, e a que preço.
/// </summary>
/// <remarks>
/// Antes deste passo a carreira tinha um verbo só. O jogador escolhia a luta e
/// todo o resto — evolução, declínio, estilo — acontecia sozinho na virada do
/// ano. O camp devolve a ele a segunda decisão: para onde o lutador cresce.
/// <para>
/// O teto de potencial é o mesmo da <see cref="CurvaDeEvolucao"/>, e de
/// propósito. Se o camp pudesse furar o limite do draft, bastaria treinar
/// wrestling por dez anos para que qualquer montagem virasse a mesma; com o
/// teto, o camp escolhe <b>onde</b> o crescimento acontece, nunca quanto.
/// </para>
/// </remarks>
public static class CampoDeTreino
{
    /// <summary>Chance de ganhar o ponto, antes do ajuste por idade.</summary>
    private static readonly Dictionary<IntensidadeDoTreino, double> ChanceBase = new()
    {
        [IntensidadeDoTreino.Leve] = 0.0,
        [IntensidadeDoTreino.Padrao] = 0.35,
        [IntensidadeDoTreino.Pesado] = 0.65
    };

    /// <summary>
    /// Quanto o corpo ainda aprende. Não é o mesmo que a curva de evolução, que
    /// trata do que a idade dá e tira sozinha: aqui é o quanto o treino ainda
    /// converte em nota.
    /// </summary>
    private static double AproveitamentoPorIdade(int idade) => idade switch
    {
        <= 24 => 1.2,
        <= 29 => 1.0,
        <= 34 => 0.7,
        _ => 0.45
    };

    /// <summary>
    /// Roda o camp e devolve o que ele produziu.
    /// </summary>
    /// <param name="atributos">Atributos com que o lutador entrou no camp.</param>
    /// <param name="atributosDeEstreia">
    /// Atributos do draft, que definem o teto de cada habilidade.
    /// </param>
    /// <param name="foco">
    /// A habilidade escolhida, ou <c>null</c> quando o jogador não escolheu
    /// nenhuma — o que é uma escolha válida: o camp acontece, só não puxa nada.
    /// </param>
    public static ResultadoDoCamp Rodar(
        Atributos atributos,
        Atributos atributosDeEstreia,
        Habilidade? foco,
        IntensidadeDoTreino intensidade,
        int idade,
        Sorteio sorteio)
    {
        ArgumentNullException.ThrowIfNull(atributos);
        ArgumentNullException.ThrowIfNull(atributosDeEstreia);
        ArgumentNullException.ThrowIfNull(sorteio);

        if (foco is not { } habilidade)
        {
            return ResultadoDoCamp.SemFoco(intensidade);
        }

        var notaAtual = atributos[habilidade];
        var teto = Math.Min(
            atributosDeEstreia[habilidade] + CurvaDeEvolucao.TetoDeEvolucaoPorHabilidade,
            Atributos.NotaMaxima);

        if (notaAtual >= teto)
        {
            return ResultadoDoCamp.NoTeto(habilidade, intensidade, notaAtual);
        }

        var chance = ChanceBase[intensidade] * AproveitamentoPorIdade(idade);

        if (!sorteio.Acontece(chance))
        {
            return ResultadoDoCamp.SemGanho(habilidade, intensidade, notaAtual);
        }

        var evoluidos = atributos.ComAjustes(new Dictionary<Habilidade, int> { [habilidade] = 1 });

        return ResultadoDoCamp.ComGanho(habilidade, intensidade, notaAtual, evoluidos);
    }
}

/// <summary>
/// O que o camp produziu, na forma em que a tela precisa contar.
/// </summary>
/// <remarks>
/// Carrega os atributos novos em vez de alterar o estado por dentro. Quem
/// escreve no estado da carreira é o motor, em um lugar só — um método de
/// simulação que muda estado alheio é o tipo de coisa que só se descobre
/// quando dois deles rodam na mesma jogada.
/// </remarks>
/// <param name="AtributosDepois">
/// Os atributos ao fim do camp, ou <c>null</c> quando nada mudou.
/// </param>
public sealed record ResultadoDoCamp(
    Habilidade? Foco,
    IntensidadeDoTreino Intensidade,
    bool Evoluiu,
    bool NoTetoDoPotencial,
    int NotaAntes,
    int NotaDepois,
    Atributos? AtributosDepois)
{
    internal static ResultadoDoCamp SemFoco(IntensidadeDoTreino intensidade) =>
        new(null, intensidade, false, false, 0, 0, null);

    internal static ResultadoDoCamp SemGanho(Habilidade foco, IntensidadeDoTreino intensidade, int nota) =>
        new(foco, intensidade, false, false, nota, nota, null);

    internal static ResultadoDoCamp NoTeto(Habilidade foco, IntensidadeDoTreino intensidade, int nota) =>
        new(foco, intensidade, false, true, nota, nota, null);

    internal static ResultadoDoCamp ComGanho(
        Habilidade foco,
        IntensidadeDoTreino intensidade,
        int notaAntes,
        Atributos depois) =>
        new(foco, intensidade, true, false, notaAntes, depois[foco], depois);
}
