using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Vantagem que um estilo tem sobre outro, expressa como multiplicador da
/// ofensiva.
/// </summary>
/// <remarks>
/// É o que permite um lutador de overall menor vencer um favorito: MMA é jogo
/// de pedra-papel-tesoura, não de nota geral. O ajuste aqui é deliberadamente
/// pequeno — de 6% a 10% — porque o grosso da vantagem de estilo já aparece
/// naturalmente nas fórmulas do <see cref="MotorDeLuta"/>: um wrestler já
/// derruba mais um striker de wrestling baixo sem precisar de bônus nenhum.
/// Esta tabela representa o resto: leitura de jogo, preparação específica e o
/// desconforto de enfrentar um problema que você não treina.
/// </remarks>
public static class MatchupDeEstilos
{
    /// <summary>Multiplicador quando não há vantagem definida entre os dois estilos.</summary>
    public const double SemVantagem = 1.00;

    private const double VantagemForte = 1.10;
    private const double VantagemLeve = 1.06;
    private const double DesvantagemLeve = 0.95;

    /// <summary>
    /// Pares (atacante, defensor) com vantagem definida. Tudo que não estiver
    /// aqui vale <see cref="SemVantagem"/> — a tabela lista só o que é
    /// verdade, em vez de preencher 36 combinações com 1,00.
    /// </summary>
    private static readonly Dictionary<(EstiloDeLuta Atacante, EstiloDeLuta Defensor), double> Tabela = new()
    {
        // Quem leva a luta para o chão pune quem só quer trocar.
        [(EstiloDeLuta.WrestlerDePressao, EstiloDeLuta.Nocauteador)] = VantagemForte,
        [(EstiloDeLuta.WrestlerDePressao, EstiloDeLuta.LutadorDeMovimentacao)] = VantagemForte,
        [(EstiloDeLuta.GrapplerCompleto, EstiloDeLuta.Nocauteador)] = VantagemForte,
        [(EstiloDeLuta.GrapplerCompleto, EstiloDeLuta.ContraGolpeadorTecnico)] = VantagemLeve,

        // O grappler espera a queda do wrestler para finalizar por baixo.
        [(EstiloDeLuta.GrapplerCompleto, EstiloDeLuta.WrestlerDePressao)] = VantagemLeve,

        // O contra-golpeador vive do erro de quem avança sem paciência.
        [(EstiloDeLuta.ContraGolpeadorTecnico, EstiloDeLuta.Nocauteador)] = VantagemForte,
        [(EstiloDeLuta.ContraGolpeadorTecnico, EstiloDeLuta.LutadorDeMovimentacao)] = VantagemLeve,

        // Movimentação castiga quem depende de acertar o golpe pesado.
        [(EstiloDeLuta.LutadorDeMovimentacao, EstiloDeLuta.Nocauteador)] = VantagemLeve,

        // O nocauteador só precisa de um golpe: quem circula ou cadencia sofre.
        [(EstiloDeLuta.Nocauteador, EstiloDeLuta.WrestlerDePressao)] = VantagemLeve,

        // Trocar com quem quer trocar é fazer o jogo do adversário.
        [(EstiloDeLuta.Nocauteador, EstiloDeLuta.ContraGolpeadorTecnico)] = DesvantagemLeve,
        [(EstiloDeLuta.LutadorDeMovimentacao, EstiloDeLuta.WrestlerDePressao)] = DesvantagemLeve,

        // Sem buracos e sem vantagens: o lutador completo neutraliza especialistas.
        [(EstiloDeLuta.LutadorCompleto, EstiloDeLuta.Nocauteador)] = VantagemLeve,
        [(EstiloDeLuta.LutadorCompleto, EstiloDeLuta.GrapplerCompleto)] = VantagemLeve
    };

    /// <summary>
    /// Multiplicador da ofensiva de <paramref name="atacante"/> contra
    /// <paramref name="defensor"/>.
    /// </summary>
    public static double Vantagem(EstiloDeLuta atacante, EstiloDeLuta defensor) =>
        Tabela.GetValueOrDefault((atacante, defensor), SemVantagem);
}
