using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Simulation;

public sealed class MotorDeCarreiraTeste
{
    private readonly MotorDeCarreira _motor = new(new MotorDeLuta(), new GeradorDeAdversarios());

    [Fact]
    public void AMesmaSementeProduzSempreAMesmaCarreira()
    {
        var primeira = _motor.Simular(Cenario.PartidaComDraftConcluido(seed: 20260805));
        var segunda = _motor.Simular(Cenario.PartidaComDraftConcluido(seed: 20260805));

        segunda.Cartel.Should().Be(primeira.Cartel);
        segunda.Legado.Should().Be(primeira.Legado);
        segunda.PontuacaoDeLegado.Should().Be(primeira.PontuacaoDeLegado);
        segunda.IdadeDeAposentadoria.Should().Be(primeira.IdadeDeAposentadoria);
        segunda.Lutas.Should().BeEquivalentTo(primeira.Lutas);
    }

    [Fact]
    public void OCartelSempreFechaComOTotalDeLutas()
    {
        var carreira = _motor.Simular(Cenario.PartidaComDraftConcluido(Cenario.Atributos(88), seed: 7));

        (carreira.Vitorias + carreira.Derrotas + carreira.Empates).Should().Be(carreira.TotalDeLutas);
    }

    [Fact]
    public void OsMetodosDeVitoriaSomamOTotalDeVitorias()
    {
        var carreira = _motor.Simular(Cenario.PartidaComDraftConcluido(Cenario.Atributos(88), seed: 7));

        (carreira.VitoriasPorNocaute + carreira.VitoriasPorFinalizacao + carreira.VitoriasPorDecisao)
            .Should().Be(carreira.Vitorias);
    }

    [Fact]
    public void TodaCarreiraTemAoMenosUmaLutaEUmVeredito()
    {
        // Mesmo estreando na idade máxima permitida, o lutador dispõe de uma
        // temporada: o laço do motor é do-while justamente por isso.
        var ficha = Cenario.Ficha(idadeInicial: FichaDeInscricao.IdadeMaxima);
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(75), seed: 3, ficha: ficha);

        var carreira = _motor.Simular(partida);

        carreira.TotalDeLutas.Should().BeGreaterThan(0);
        carreira.Legado.Should().BeDefined();
    }

    [Fact]
    public void NinguemLutaDepoisDosQuarentaAnos()
    {
        var idades = Enumerable.Range(1, 60)
            .Select(semente => _motor.Simular(Cenario.PartidaComDraftConcluido(seed: semente)))
            .SelectMany(carreira => carreira.Lutas.Select(luta => luta.Idade))
            .ToList();

        idades.Should().OnlyContain(idade => idade <= 40);
    }

    [Fact]
    public void AsLutasSaoRegistradasEmOrdemCronologica()
    {
        var carreira = _motor.Simular(Cenario.PartidaComDraftConcluido(Cenario.Atributos(90), seed: 11));

        carreira.Lutas.Select(luta => luta.Ordem).Should().BeInAscendingOrder();
        carreira.Lutas.Select(luta => luta.Idade).Should().BeInAscendingOrder();
    }

    [Fact]
    public void LutasDeCinturaoSaoSempreDeCincoRounds()
    {
        var lutasDeTitulo = SimularVariasCarreiras(Cenario.Atributos(94), quantidade: 40)
            .SelectMany(carreira => carreira.Lutas)
            .Where(luta => luta.ValendoCinturao)
            .ToList();

        lutasDeTitulo.Should().NotBeEmpty();
        lutasDeTitulo.Should().OnlyContain(luta => luta.RoundsProgramados == 5);
    }

    [Fact]
    public void LutasComunsSaoSempreDeTresRounds()
    {
        var lutasComuns = SimularVariasCarreiras(Cenario.Atributos(85), quantidade: 20)
            .SelectMany(carreira => carreira.Lutas)
            .Where(luta => !luta.ValendoCinturao)
            .ToList();

        lutasComuns.Should().OnlyContain(luta => luta.RoundsProgramados == 3);
    }

    [Fact]
    public void UmaCarreiraDominanteChegaAoCinturaoEDefendeOTitulo()
    {
        var carreiras = SimularVariasCarreiras(Cenario.Atributos(96), quantidade: 30);

        carreiras.Should().Contain(carreira => carreira.FoiCampeao);
        carreiras.Should().Contain(carreira => carreira.DefesasDeCinturao > 0);
        carreiras.Where(carreira => carreira.FoiCampeao).Should()
            .OnlyContain(carreira => carreira.AnosComoCampeao >= 0);
    }

    [Fact]
    public void UmCampeaoConsolidadoEventualmenteSobeDeCategoria()
    {
        var carreiras = SimularVariasCarreiras(Cenario.Atributos(96), quantidade: 40);

        var comMudancaDeCategoria = carreiras
            .Where(carreira => carreira.Lutas.Select(luta => luta.Categoria).Distinct().Count() > 1)
            .ToList();

        comMudancaDeCategoria.Should().NotBeEmpty();
        comMudancaDeCategoria.Should().Contain(carreira => carreira.FoiDuploCampeao);
    }

    [Fact]
    public void OLutadorEstreiaNoCircuitoRegionalENaoNaGrandeOrganizacao()
    {
        var carreira = _motor.Simular(Cenario.PartidaComDraftConcluido(Cenario.Atributos(90), seed: 5));

        carreira.Lutas[0].Organizacao.Should().Be(NivelDaOrganizacao.CircuitoRegional);
    }

    [Fact]
    public void UmBuildFracoNuncaChegaAoCinturao()
    {
        var carreiras = SimularVariasCarreiras(Cenario.Atributos(65), quantidade: 40);

        carreiras.Should().OnlyContain(carreira => !carreira.FoiCampeao);
    }

    private List<Carreira> SimularVariasCarreiras(Atributos atributos, int quantidade) =>
        Enumerable.Range(1, quantidade)
            .Select(semente => _motor.Simular(Cenario.PartidaComDraftConcluido(atributos, seed: semente)))
            .ToList();
}
