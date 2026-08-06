using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Domain.Rules;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Domain;

/// <summary>Cálculo de overall, identificação de estilo e invariantes dos atributos.</summary>
public sealed class RegrasDoJogoTeste
{
    [Fact]
    public void OsPesosDoOverallSomamExatamenteUm()
    {
        CalculadoraDeOverall.Pesos.Values.Sum().Should().Be(1m);
    }

    [Fact]
    public void OverallDeAtributosIguaisEIgualAoProprioValor()
    {
        // Consequência direta dos pesos somarem 1: se tudo vale 80, o overall é 80.
        CalculadoraDeOverall.Calcular(Cenario.Atributos(80)).Should().Be(80m);
    }

    [Fact]
    public void OverallPonderaCadaHabilidadePeloSeuPeso()
    {
        // Striking pesa 0,15: subir de 80 para 100 acrescenta 20 x 0,15 = 3.
        var atributos = Cenario.Atributos(80, striking: 100);

        CalculadoraDeOverall.Calcular(atributos).Should().Be(83m);
    }

    [Fact]
    public void OverallDoExemploDaDocumentacaoBateComOEsperado()
    {
        var lutadorDoReadme = new Atributos(
            striking: 97, potencia: 99, velocidade: 95, wrestling: 99,
            jiuJitsu: 99, cardio: 98, resistencia: 96, inteligenciaDeLuta: 99);

        CalculadoraDeOverall.Calcular(lutadorDoReadme).Should().BeInRange(97m, 98m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void RecusaNotaForaDaEscala(int notaInvalida)
    {
        var criacaoInvalida = () => Cenario.Atributos(padrao: 80, potencia: notaInvalida);

        criacaoInvalida.Should().Throw<DadoInvalidoException>().WithMessage("*Potência*entre 1 e 100*");
    }

    [Fact]
    public void RecusaMontarAtributosComHabilidadeFaltando()
    {
        var incompleto = new Dictionary<Habilidade, int> { [Habilidade.Striking] = 90 };

        var montagemInvalida = () => Atributos.APartirDe(incompleto);

        montagemInvalida.Should().Throw<DadoInvalidoException>().WithMessage("*Faltam notas*");
    }

    [Fact]
    public void AjustesNaoEstouramAEscalaDeNotas()
    {
        var atributos = Cenario.Atributos(98);

        var ajustado = atributos.ComAjustes(new Dictionary<Habilidade, int>
        {
            [Habilidade.Striking] = 20,
            [Habilidade.Cardio] = -200
        });

        ajustado.Striking.Should().Be(Atributos.NotaMaxima);
        ajustado.Cardio.Should().Be(Atributos.NotaMinima);
    }

    [Fact]
    public void IdentificaMaiorQualidadeEPrincipalFraqueza()
    {
        var atributos = Cenario.Atributos(80, potencia: 99, wrestling: 60);

        atributos.MaiorHabilidade().Should().Be(Habilidade.Potencia);
        atributos.MenorHabilidade().Should().Be(Habilidade.Wrestling);
    }

    [Theory]
    [InlineData(EstiloDeLuta.Nocauteador)]
    [InlineData(EstiloDeLuta.GrapplerCompleto)]
    [InlineData(EstiloDeLuta.WrestlerDePressao)]
    [InlineData(EstiloDeLuta.ContraGolpeadorTecnico)]
    [InlineData(EstiloDeLuta.LutadorDeMovimentacao)]
    public void IdentificaOEstiloPeloAtributoPrimario(EstiloDeLuta estiloEsperado)
    {
        var atributos = AtributosQueDestacam(estiloEsperado);

        IdentificadorDeEstilo.Identificar(atributos).Should().Be(estiloEsperado);
    }

    [Fact]
    public void ClassificaComoCompletoQuandoOsAtributosSaoEquilibrados()
    {
        var equilibrado = Cenario.Atributos(85, potencia: 88, wrestling: 82);

        equilibrado.Amplitude().Should().BeLessThanOrEqualTo(IdentificadorDeEstilo.AmplitudeMaximaParaCompleto);
        IdentificadorDeEstilo.Identificar(equilibrado).Should().Be(EstiloDeLuta.LutadorCompleto);
    }

    /// <summary>Monta atributos com o par primário/secundário do estilo bem acima do resto.</summary>
    private static Atributos AtributosQueDestacam(EstiloDeLuta estilo) => estilo switch
    {
        EstiloDeLuta.Nocauteador => Cenario.Atributos(65, potencia: 98, striking: 92),
        EstiloDeLuta.GrapplerCompleto => Cenario.Atributos(65, jiuJitsu: 98, wrestling: 92),
        EstiloDeLuta.WrestlerDePressao => Cenario.Atributos(65, wrestling: 98, cardio: 92),
        EstiloDeLuta.ContraGolpeadorTecnico => Cenario.Atributos(65, inteligenciaDeLuta: 98, striking: 92),
        EstiloDeLuta.LutadorDeMovimentacao => Cenario.Atributos(65, velocidade: 98, striking: 92),
        _ => Cenario.Atributos()
    };
}
