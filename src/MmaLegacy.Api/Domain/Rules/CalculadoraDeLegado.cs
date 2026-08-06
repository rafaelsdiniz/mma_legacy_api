using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// Transforma uma carreira encerrada no veredito final.
/// </summary>
/// <remarks>
/// A pontuação e o nível respondem a perguntas diferentes, por isso são duas
/// coisas. A <b>pontuação</b> é contínua e serve para ordenar rankings — ela
/// distingue duas carreiras parecidas. O <b>nível</b> é o rótulo que o jogador
/// lê no fim, e ele depende sobretudo do que foi conquistado: um cinturão vale
/// mais como título do que como pontos, e nenhuma quantidade de vitórias em
/// evento regional transforma alguém em campeão mundial.
/// </remarks>
public static class CalculadoraDeLegado
{
    private const int PontosPorVitoria = 3;
    private const int PontosPorDerrota = -4;
    private const int PontosPorCinturao = 25;
    private const int PontosPorDefesaDeTitulo = 8;
    private const int PontosPorLutaNaSequencia = 2;
    private const int BonusPorCarreiraInvicta = 30;
    private const int BonusPorDuploCampeonato = 40;

    /// <summary>
    /// Overall a partir do qual um adversário conta como "qualidade". Abaixo
    /// disso é o nível médio do circuito regional, e vencer não deve pontuar
    /// além da própria vitória.
    /// </summary>
    private const decimal OverallDeReferenciaDoAdversario = 70m;

    /// <summary>Quanto cada ponto de overall do adversário derrotado acrescenta.</summary>
    private const decimal PesoDaQualidadeDoAdversario = 0.5m;

    /// <summary>Defesas de cinturão a partir das quais o reinado vira dominância.</summary>
    private const int DefesasParaReinadoDominante = 5;

    private const int PontuacaoParaMaiorDeTodos = 320;
    private const int PontuacaoParaLenda = 260;
    private const int PontuacaoParaElite = 90;
    private const int PontuacaoParaVeterano = 50;
    private const int VitoriasParaLutadorRegional = 5;

    /// <summary>
    /// Calcula a pontuação e o nível e grava os dois na carreira.
    /// </summary>
    public static void Aplicar(Carreira carreira)
    {
        ArgumentNullException.ThrowIfNull(carreira);

        var pontuacao = CalcularPontuacao(carreira);
        carreira.DefinirLegado(ClassificarNivel(carreira, pontuacao), pontuacao);
    }

    /// <summary>Pontuação bruta da carreira. Nunca é negativa.</summary>
    public static int CalcularPontuacao(Carreira carreira)
    {
        ArgumentNullException.ThrowIfNull(carreira);

        decimal pontuacao =
            (carreira.Vitorias * PontosPorVitoria) +
            (carreira.Derrotas * PontosPorDerrota) +
            (carreira.CinturoesConquistados * PontosPorCinturao) +
            (carreira.DefesasDeCinturao * PontosPorDefesaDeTitulo) +
            (carreira.MaiorSequenciaDeVitorias * PontosPorLutaNaSequencia) +
            CalcularBonusDeQualidade(carreira);

        if (carreira.AposentouInvicto)
        {
            pontuacao += BonusPorCarreiraInvicta;
        }

        if (carreira.FoiDuploCampeao)
        {
            pontuacao += BonusPorDuploCampeonato;
        }

        return Math.Max(0, (int)Math.Round(pontuacao, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Recompensa quem venceu gente boa. Vitórias sobre adversários abaixo da
    /// referência não somam nada aqui — já valeram os pontos por vitória.
    /// </summary>
    private static decimal CalcularBonusDeQualidade(Carreira carreira) =>
        carreira.Lutas
            .Where(luta => luta.Resultado == ResultadoDaLuta.Vitoria)
            .Sum(luta => Math.Max(0m, luta.OverallDoAdversario - OverallDeReferenciaDoAdversario))
        * PesoDaQualidadeDoAdversario;

    /// <summary>
    /// A escada de legado, avaliada de cima para baixo: vence o primeiro degrau
    /// cuja condição a carreira satisfaz.
    /// </summary>
    private static NivelDeLegado ClassificarNivel(Carreira carreira, int pontuacao)
    {
        var disputouCinturao = carreira.Lutas.Any(luta => luta.DisputaDeCinturao);
        var venceuEmGrandeOrganizacao = carreira.Lutas.Any(luta =>
            luta.Organizacao == NivelDaOrganizacao.GrandeOrganizacao &&
            luta.Resultado == ResultadoDaLuta.Vitoria);

        var escada = new (bool Condicao, NivelDeLegado Nivel)[]
        {
            (carreira.FoiDuploCampeao && pontuacao >= PontuacaoParaMaiorDeTodos, NivelDeLegado.MaiorDeTodosOsTempos),
            (pontuacao >= PontuacaoParaLenda, NivelDeLegado.LendaDoMma),
            (carreira.FoiDuploCampeao, NivelDeLegado.DuploCampeao),
            (carreira.DefesasDeCinturao >= DefesasParaReinadoDominante, NivelDeLegado.CampeaoDominante),
            (carreira.FoiCampeao, NivelDeLegado.CampeaoMundial),
            (disputouCinturao, NivelDeLegado.DesafianteAoCinturao),
            (venceuEmGrandeOrganizacao && pontuacao >= PontuacaoParaElite, NivelDeLegado.CompetidorDeElite),
            (pontuacao >= PontuacaoParaVeterano, NivelDeLegado.VeteranoRespeitado),
            (carreira.Vitorias >= VitoriasParaLutadorRegional, NivelDeLegado.LutadorRegional)
        };

        foreach (var degrau in escada)
        {
            if (degrau.Condicao)
            {
                return degrau.Nivel;
            }
        }

        return NivelDeLegado.PromessaQueNaoCorrespondeu;
    }
}
