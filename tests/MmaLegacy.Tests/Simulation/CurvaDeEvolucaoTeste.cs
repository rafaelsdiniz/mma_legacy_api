using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Simulation;

public sealed class CurvaDeEvolucaoTeste
{
    private const int NotaDeEstreia = 70;

    [Fact]
    public void UmLutadorJovemEvoluiTecnicamente()
    {
        var estreia = Cenario.Atributos(NotaDeEstreia);

        var aposUmAno = CurvaDeEvolucao.AplicarAno(estreia, estreia, idade: 20, 0, new Sorteio(1));

        aposUmAno.Striking.Should().BeGreaterThan(estreia.Striking);
        aposUmAno.Wrestling.Should().BeGreaterThan(estreia.Wrestling);
        aposUmAno.JiuJitsu.Should().BeGreaterThan(estreia.JiuJitsu);
    }

    [Fact]
    public void UmVeteranoPerdeVelocidadeECardio()
    {
        var estreia = Cenario.Atributos(NotaDeEstreia);

        var apos = CurvaDeEvolucao.AplicarAno(estreia, estreia, idade: 38, 0, new Sorteio(1));

        apos.Velocidade.Should().BeLessThan(estreia.Velocidade);
        apos.Cardio.Should().BeLessThan(estreia.Cardio);
    }

    [Fact]
    public void OFightIqCresceEmQualquerIdade()
    {
        var estreia = Cenario.Atributos(NotaDeEstreia);

        foreach (var idade in new[] { 19, 25, 30, 34, 39 })
        {
            var apos = CurvaDeEvolucao.AplicarAno(estreia, estreia, idade, 0, new Sorteio(idade));

            apos.InteligenciaDeLuta.Should()
                .BeGreaterThan(estreia.InteligenciaDeLuta, $"a experiência não some aos {idade} anos");
        }
    }

    [Fact]
    public void NenhumaHabilidadeUltrapassaOTetoDePotencial()
    {
        var estreia = Cenario.Atributos(NotaDeEstreia);
        var atuais = estreia;

        // Vinte anos seguidos na faixa de maior evolução: o pior caso possível.
        for (var ano = 0; ano < 20; ano++)
        {
            atuais = CurvaDeEvolucao.AplicarAno(atuais, estreia, idade: 20, 0, new Sorteio(ano));
        }

        var teto = NotaDeEstreia + CurvaDeEvolucao.TetoDeEvolucaoPorHabilidade;
        atuais.Listar().Should().OnlyContain(par => par.Value <= teto);
    }

    [Fact]
    public void NocautesSofridosCobramResistenciaAlemDaIdade()
    {
        var estreia = Cenario.Atributos(NotaDeEstreia);

        var semGuerra = CurvaDeEvolucao.AplicarAno(estreia, estreia, idade: 30, 0, new Sorteio(42));
        var comGuerras = CurvaDeEvolucao.AplicarAno(estreia, estreia, idade: 30, 3, new Sorteio(42));

        comGuerras.Resistencia.Should().BeLessThan(semGuerra.Resistencia);
    }

    [Fact]
    public void ODeclinioNaoRespeitaTetoNenhumAlemDaEscalaDeNotas()
    {
        var estreia = Cenario.Atributos(padrao: 5);
        var atuais = estreia;

        for (var ano = 0; ano < 10; ano++)
        {
            atuais = CurvaDeEvolucao.AplicarAno(atuais, estreia, idade: 39, 2, new Sorteio(ano));
        }

        atuais.Listar().Should().OnlyContain(par => par.Value >= Atributos.NotaMinima);
    }
}
