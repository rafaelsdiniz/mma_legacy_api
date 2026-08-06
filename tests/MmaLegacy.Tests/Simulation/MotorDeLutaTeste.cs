using FluentAssertions;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Simulation;

public sealed class MotorDeLutaTeste
{
    private readonly MotorDeLuta _motor = new();

    [Fact]
    public void AMesmaSementeProduzSempreAMesmaLuta()
    {
        var azul = PerfilDeCombate.Montar("Azul", Cenario.Atributos(88));
        var vermelho = PerfilDeCombate.Montar("Vermelho", Cenario.Atributos(82));

        var primeira = _motor.Simular(azul, vermelho, 3, new Sorteio(20260805));
        var segunda = _motor.Simular(azul, vermelho, 3, new Sorteio(20260805));

        segunda.Should().BeEquivalentTo(primeira);
    }

    [Fact]
    public void SementesDiferentesProduzemLutasDiferentes()
    {
        var azul = PerfilDeCombate.Montar("Azul", Cenario.Atributos(85));
        var vermelho = PerfilDeCombate.Montar("Vermelho", Cenario.Atributos(85));

        var desfechos = Enumerable.Range(1, 50)
            .Select(semente => _motor.Simular(azul, vermelho, 3, new Sorteio(semente)))
            .ToList();

        desfechos.Select(desfecho => desfecho.Resultado).Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void LutadorMuitoSuperiorVenceAGrandeMaioriaSemNuncaSerImbativel()
    {
        var favorito = PerfilDeCombate.Montar("Favorito", Cenario.Atributos(95));
        var azarao = PerfilDeCombate.Montar("Azarao", Cenario.Atributos(68));

        var vitorias = ContarVitorias(favorito, azarao, rounds: 3, amostras: 500);

        // Domínio claro, mas a curva logística garante que nenhuma luta seja
        // impossível de vencer: zebra continua existindo.
        vitorias.Should().BeGreaterThan(350).And.BeLessThan(500);
    }

    [Fact]
    public void WrestlerLevaVantagemSobreStrikerComDefesaDeQuedaRuim()
    {
        var wrestler = PerfilDeCombate.Montar(
            "Wrestler",
            Cenario.Atributos(78, wrestling: 96, cardio: 92, jiuJitsu: 88));

        // A velocidade alta compensa no overall o buraco de wrestling, para que
        // os dois cheguem à luta com a mesma nota geral.
        var striker = PerfilDeCombate.Montar(
            "Striker",
            Cenario.Atributos(78, striking: 96, potencia: 94, velocidade: 92, wrestling: 58));

        var vitoriasDoWrestler = ContarVitorias(wrestler, striker, rounds: 3, amostras: 500);

        // Os dois têm overall parecido: a diferença vem do matchup, não da nota.
        wrestler.Overall.Should().BeApproximately(striker.Overall, 3m);
        vitoriasDoWrestler.Should().BeGreaterThan(250);
    }

    [Fact]
    public void CardioAltoRendeMaisEmCincoRoundsDoQueEmTres()
    {
        var maratonista = PerfilDeCombate.Montar("Maratonista", Cenario.Atributos(82, cardio: 99));
        var explosivo = PerfilDeCombate.Montar("Explosivo", Cenario.Atributos(82, cardio: 62, potencia: 95));

        var vitoriasEmTres = ContarVitorias(maratonista, explosivo, rounds: 3, amostras: 600);
        var vitoriasEmCinco = ContarVitorias(maratonista, explosivo, rounds: 5, amostras: 600);

        // A fadiga se acumula round a round, então o desgaste do adversário de
        // cardio baixo só cobra o preço nas lutas longas.
        vitoriasEmCinco.Should().BeGreaterThan(vitoriasEmTres);
    }

    [Fact]
    public void NenhumLutadorTemVantagemPorSerOPrimeiroArgumento()
    {
        var perfil = PerfilDeCombate.Montar("Identico", Cenario.Atributos(85));

        var vitorias = ContarVitorias(perfil, perfil, rounds: 3, amostras: 2000);

        // Entre perfis idênticos o resultado precisa ser simétrico. Um desvio
        // aqui denunciaria viés na ordem de resolução do encerramento.
        vitorias.Should().BeInRange(880, 1120);
    }

    [Fact]
    public void DecisaoSempreRegistraOUltimoRoundProgramado()
    {
        var azul = PerfilDeCombate.Montar("Azul", Cenario.Atributos(85));
        var vermelho = PerfilDeCombate.Montar("Vermelho", Cenario.Atributos(85));

        var decisoes = Enumerable.Range(1, 300)
            .Select(semente => _motor.Simular(azul, vermelho, 5, new Sorteio(semente)))
            .Where(desfecho => desfecho.Metodo == MetodoDeEncerramento.Decisao)
            .ToList();

        decisoes.Should().NotBeEmpty();
        decisoes.Should().OnlyContain(desfecho => desfecho.RoundDoEncerramento == 5);
    }

    [Fact]
    public void FinalizacaoENocauteAcontecemDentroDosRoundsProgramados()
    {
        var grappler = PerfilDeCombate.Montar("Grappler", Cenario.Atributos(80, jiuJitsu: 98, wrestling: 95));
        var vitima = PerfilDeCombate.Montar("Vitima", Cenario.Atributos(80, jiuJitsu: 55, wrestling: 55));

        var desfechos = Enumerable.Range(1, 300)
            .Select(semente => _motor.Simular(grappler, vitima, 3, new Sorteio(semente)))
            .ToList();

        desfechos.Should().OnlyContain(desfecho => desfecho.RoundDoEncerramento >= 1);
        desfechos.Should().OnlyContain(desfecho => desfecho.RoundDoEncerramento <= 3);
        desfechos.Should().Contain(desfecho => desfecho.Metodo == MetodoDeEncerramento.Finalizacao);
    }

    private int ContarVitorias(PerfilDeCombate azul, PerfilDeCombate vermelho, int rounds, int amostras) =>
        Enumerable.Range(1, amostras)
            .Count(semente => _motor.Simular(azul, vermelho, rounds, new Sorteio(semente)).Resultado
                              == ResultadoDaLuta.Vitoria);
}
