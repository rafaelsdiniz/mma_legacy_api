using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Simulation;

/// <summary>
/// O camp que antecede a luta: para onde o lutador cresce, e a que preço.
/// </summary>
public sealed class CampoDeTreinoTeste
{
    private static readonly Atributos Estreia = Cenario.Atributos(80);

    [Fact]
    public void CampSemFocoNaoMexeEmNada()
    {
        var camp = Rodar(foco: null, IntensidadeDoTreino.Pesado, semente: 1);

        camp.Foco.Should().BeNull();
        camp.Evoluiu.Should().BeFalse();
        camp.AtributosDepois.Should().BeNull();
    }

    [Fact]
    public void CampLeveNuncaEvolui()
    {
        var evolucoes = Enumerable.Range(1, 200)
            .Count(semente => Rodar(Habilidade.Wrestling, IntensidadeDoTreino.Leve, semente).Evoluiu);

        evolucoes.Should().Be(0, "camp leve é manutenção, e manutenção não constrói nada");
    }

    [Fact]
    public void CampPesadoEvoluiMaisQueOPadrao()
    {
        var padrao = ContarEvolucoes(IntensidadeDoTreino.Padrao);
        var pesado = ContarEvolucoes(IntensidadeDoTreino.Pesado);

        pesado.Should().BeGreaterThan(padrao);
    }

    [Fact]
    public void OCampRespeitaOTetoDoPotencialDoDraft()
    {
        var teto = Estreia[Habilidade.Wrestling] + CurvaDeEvolucao.TetoDeEvolucaoPorHabilidade;
        var atributos = Cenario.Atributos(80, wrestling: teto);

        var camp = CampoDeTreino.Rodar(
            atributos,
            Estreia,
            Habilidade.Wrestling,
            IntensidadeDoTreino.Pesado,
            idade: 22,
            new Sorteio(1));

        camp.Evoluiu.Should().BeFalse();
        camp.NoTetoDoPotencial.Should().BeTrue(
            "estar no teto e simplesmente não render são resultados iguais na nota e opostos na decisão");
    }

    [Fact]
    public void CorpoVelhoAproveitaMenosOTreino()
    {
        var jovem = ContarEvolucoes(IntensidadeDoTreino.Pesado, idade: 22);
        var veterano = ContarEvolucoes(IntensidadeDoTreino.Pesado, idade: 37);

        veterano.Should().BeLessThan(jovem);
    }

    [Theory]
    [InlineData(IntensidadeDoTreino.Leve)]
    [InlineData(IntensidadeDoTreino.Pesado)]
    public void AIntensidadeMexeNoRiscoDeLesao(IntensidadeDoTreino intensidade)
    {
        var padrao = CalculadoraDeLesao.Risco(GrauDeDificuldade.Dura, 27, 80);
        var comparado = CalculadoraDeLesao.Risco(GrauDeDificuldade.Dura, 27, 80, intensidade);

        if (intensidade == IntensidadeDoTreino.Leve)
        {
            comparado.Should().BeLessThan(padrao, "camp leve poupa o corpo");
            return;
        }

        comparado.Should().BeGreaterThan(padrao, "camp pesado entrega um lutador já castigado");
    }

    /// <summary>
    /// A prova de que o camp chega ao octógono: quem treinou pesado a carreira
    /// inteira chega a um pico mais alto do que quem só se manteve.
    /// </summary>
    [Fact]
    public void TreinarPesadoLevaOLutadorAUmPicoMaisAlto()
    {
        var comTreinoPesado = PicoMedio(IntensidadeDoTreino.Pesado);
        var soManutencao = PicoMedio(IntensidadeDoTreino.Leve);

        comTreinoPesado.Should().BeGreaterThan(soManutencao);
    }

    private static decimal PicoMedio(IntensidadeDoTreino intensidade)
    {
        var motor = Cenario.Motor();
        var picos = new List<decimal>();

        foreach (var semente in Enumerable.Range(1, 12))
        {
            var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(82), seed: semente);
            var carreira = motor.Iniciar(partida, RankingDoJogo.Vazio);

            while (!carreira.Encerrada)
            {
                if (carreira.Estado.EstaLesionado)
                {
                    motor.Recuperar(partida, carreira, RankingDoJogo.Vazio);
                    continue;
                }

                motor.Aceitar(
                    partida,
                    carreira,
                    RankingDoJogo.Vazio,
                    carreira.Ofertas[0].Indice,
                    carreira.Estado.Atributos.MenorHabilidade(),
                    intensidade);
            }

            picos.Add(carreira.OverallMaximo);
        }

        return picos.Sum() / picos.Count;
    }

    private static int ContarEvolucoes(IntensidadeDoTreino intensidade, int idade = 25) =>
        Enumerable.Range(1, 300)
            .Count(semente => CampoDeTreino.Rodar(
                Cenario.Atributos(80),
                Estreia,
                Habilidade.Wrestling,
                intensidade,
                idade,
                new Sorteio(semente)).Evoluiu);

    private static ResultadoDoCamp Rodar(Habilidade? foco, IntensidadeDoTreino intensidade, int semente) =>
        CampoDeTreino.Rodar(Cenario.Atributos(80), Estreia, foco, intensidade, idade: 25, new Sorteio(semente));
}
