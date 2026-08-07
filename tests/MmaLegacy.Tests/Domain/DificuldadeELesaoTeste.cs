using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Domain;

/// <summary>
/// O grau de dificuldade que a oferta anuncia e o preço que ele cobra do corpo.
/// </summary>
public sealed class DificuldadeELesaoTeste
{
    [Theory]
    [InlineData(70, GrauDeDificuldade.Tranquila)]
    [InlineData(76, GrauDeDificuldade.Tranquila)]
    [InlineData(78, GrauDeDificuldade.Equilibrada)]
    [InlineData(81, GrauDeDificuldade.Equilibrada)]
    [InlineData(84, GrauDeDificuldade.Dura)]
    [InlineData(86, GrauDeDificuldade.Dura)]
    [InlineData(90, GrauDeDificuldade.Brutal)]
    public void OGrauSaiDaDistanciaEntreOsDoisLutadores(int overallDoAdversario, GrauDeDificuldade esperado) =>
        CalculadoraDeDificuldade.Calcular(overallDoAdversario, overallDoJogador: 80)
            .Should().Be(esperado);

    [Fact]
    public void LutaDeCinturaoSobeUmGrau()
    {
        var comum = CalculadoraDeDificuldade.Calcular(80, 80);
        var deCinturao = CalculadoraDeDificuldade.Calcular(80, 80, valendoCinturao: true);

        comum.Should().Be(GrauDeDificuldade.Equilibrada);
        deCinturao.Should().Be(GrauDeDificuldade.Dura);
    }

    [Fact]
    public void ORiscoDeLesaoCresceComOGrauDaLuta()
    {
        var riscos = Enum.GetValues<GrauDeDificuldade>()
            .Select(grau => CalculadoraDeLesao.Risco(grau, idade: 25, resistencia: 80))
            .ToList();

        riscos.Should().BeInAscendingOrder();
    }

    [Fact]
    public void ORiscoDeLesaoCresceDepoisDosTrinta()
    {
        var aosVinteECinco = CalculadoraDeLesao.Risco(GrauDeDificuldade.Dura, 25, 80);
        var aosTrinta = CalculadoraDeLesao.Risco(GrauDeDificuldade.Dura, 30, 80);
        var aosTrintaESete = CalculadoraDeLesao.Risco(GrauDeDificuldade.Dura, 37, 80);

        // Antes dos 30 a idade não cobra nada: os dois primeiros são iguais.
        aosTrinta.Should().Be(aosVinteECinco);
        aosTrintaESete.Should().BeGreaterThan(aosTrinta);
    }

    [Fact]
    public void CorpoMaisDuroSeMachucaMenos()
    {
        var frangoDeVidro = CalculadoraDeLesao.Risco(GrauDeDificuldade.Brutal, 28, resistencia: 50);
        var couroGrosso = CalculadoraDeLesao.Risco(GrauDeDificuldade.Brutal, 28, resistencia: 95);

        couroGrosso.Should().BeLessThan(frangoDeVidro);
    }

    [Fact]
    public void NenhumaLutaEUmaSentenca()
    {
        var pior = CalculadoraDeLesao.Risco(GrauDeDificuldade.Brutal, idade: 40, resistencia: 1);

        pior.Should().BeLessThanOrEqualTo(CalculadoraDeLesao.RiscoMaximo);
    }

    [Fact]
    public void AOfertaAnunciaOMesmoRiscoQueOMotorVaiCobrar()
    {
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(80));
        var carreira = Cenario.Motor().Iniciar(partida, RankingDoJogo.Vazio);
        var estado = carreira.Estado;
        var oferta = carreira.Ofertas[0];

        var anunciado = oferta.RiscoDeLesaoPara(estado);
        var pelaRegra = CalculadoraDeLesao.Risco(
            oferta.DificuldadeContra(estado.OverallAtual),
            estado.Idade,
            estado.Atributos[Habilidade.Resistencia]);

        anunciado.Should().Be(pelaRegra);
    }

    [Fact]
    public void ALesaoGraveCobraDoisPontosDaHabilidadeQueElaMachuca()
    {
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(80));
        var carreira = Cenario.Motor().Iniciar(partida, RankingDoJogo.Vazio);
        var estado = carreira.Estado;

        var velocidadeAntes = estado.Atributos[Habilidade.Velocidade];

        estado.Lesionar(new Lesao(TipoDeLesao.JoelhoLesionado, GravidadeDaLesao.Grave, estado.Idade));

        estado.Atributos[Habilidade.Velocidade].Should().Be(velocidadeAntes - 2);
        estado.EstaLesionado.Should().BeTrue();
        estado.LesoesSofridas.Should().Be(1);
    }

    [Fact]
    public void CorteSaraSemDeixarSequela()
    {
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(80));
        var carreira = Cenario.Motor().Iniciar(partida, RankingDoJogo.Vazio);
        var estado = carreira.Estado;
        var antes = estado.Atributos;

        estado.Lesionar(new Lesao(TipoDeLesao.Corte, GravidadeDaLesao.Leve, estado.Idade));

        estado.Atributos.Listar().Should().BeEquivalentTo(antes.Listar());
    }

    [Fact]
    public void TratarGastaCalendarioSemContarComoRecusa()
    {
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(80));
        var carreira = Cenario.Motor().Iniciar(partida, RankingDoJogo.Vazio);
        var estado = carreira.Estado;

        estado.Lesionar(new Lesao(TipoDeLesao.CostelaTrincada, GravidadeDaLesao.Moderada, estado.Idade));

        var compromissosAntes = estado.CompromissosNaTemporada;

        estado.TratarLesao().Should().BeFalse("uma lesão moderada tira o lutador de dois compromissos");
        estado.TratarLesao().Should().BeTrue("o segundo compromisso fecha a recuperação");

        estado.CompromissosNaTemporada.Should().Be(compromissosAntes + 2);
        estado.RecusasSeguidas.Should().Be(0);
        estado.EstaLesionado.Should().BeFalse();
    }

    [Fact]
    public void MachucadoNaoRecebeOfertaAteSeRecuperar()
    {
        var motor = Cenario.Motor();
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(80));
        var carreira = motor.Iniciar(partida, RankingDoJogo.Vazio);

        carreira.Estado.Lesionar(
            new Lesao(TipoDeLesao.MaoFraturada, GravidadeDaLesao.Leve, carreira.Estado.Idade));

        motor.Recuperar(partida, carreira, RankingDoJogo.Vazio);

        carreira.Estado.EstaLesionado.Should().BeFalse();
        carreira.Ofertas.Should().NotBeEmpty("recuperado, ele volta a receber luta");
    }

    /// <summary>
    /// A invariante que sustenta a tela: enquanto a carreira está viva, ou há
    /// oferta para aceitar ou há lesão para tratar. Nunca as duas, nunca
    /// nenhuma — porque qualquer um desses estados deixaria o jogador sem
    /// jogada possível.
    /// </summary>
    [Fact]
    public void CarreiraVivaSempreTemUmaJogadaPossivel()
    {
        var motor = Cenario.Motor();

        foreach (var semente in Enumerable.Range(1, 40))
        {
            var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(84), seed: semente);
            var carreira = motor.Iniciar(partida, RankingDoJogo.Vazio);

            while (!carreira.Encerrada)
            {
                var estado = carreira.Estado;

                if (estado.EstaLesionado)
                {
                    carreira.Ofertas.Should().BeEmpty("machucado não recebe luta");
                    motor.Recuperar(partida, carreira, RankingDoJogo.Vazio);
                    continue;
                }

                carreira.Ofertas.Should().NotBeEmpty("quem está inteiro sempre tem o que aceitar");
                motor.Aceitar(partida, carreira, RankingDoJogo.Vazio, carreira.Ofertas[0].Indice);
            }
        }
    }
}
