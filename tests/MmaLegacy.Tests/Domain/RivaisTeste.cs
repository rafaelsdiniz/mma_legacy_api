using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Domain;

/// <summary>
/// Os adversários de fora do ranking que a carreira lembra, e a revanche que
/// eles rendem.
/// </summary>
public sealed class RivaisTeste
{
    [Fact]
    public void OAdversarioInventadoViraRivalDepoisDaLuta()
    {
        var (partida, carreira, motor) = CarreiraFicticia(semente: 11);

        motor.Aceitar(partida, carreira, RankingDoJogo.Vazio, carreira.Ofertas[0].Indice);

        carreira.Rivais.Should().HaveCount(1);
        carreira.Rivais[0].TotalDeEncontros.Should().Be(1);
    }

    [Fact]
    public void ACarreiraLembraApenasDosQuatroMaisRecentes()
    {
        var (partida, carreira, motor) = CarreiraFicticia(semente: 11);

        for (var jogada = 0; jogada < 12 && !carreira.Encerrada; jogada++)
        {
            Jogar(partida, carreira, motor);
        }

        carreira.Rivais.Should().HaveCountLessThanOrEqualTo(4,
            "um catálogo de figurantes não é memória, é lista telefônica");
    }

    [Fact]
    public void SoTemContaAAcertarQuemEstaNaFrenteNoConfrontoDireto()
    {
        var rival = new Rival("Fulano de Teste", "12-3-0", Cenario.Atributos(80));

        rival.Anotar(ResultadoDaLuta.Derrota, MetodoDeEncerramento.Nocaute, ordemDaLuta: 1);
        rival.TemContaAAcertar.Should().BeTrue("ele venceu o jogador e o jogador não devolveu");

        rival.Anotar(ResultadoDaLuta.Vitoria, MetodoDeEncerramento.Decisao, ordemDaLuta: 4);
        rival.TemContaAAcertar.Should().BeFalse("a conta foi acertada");
    }

    [Fact]
    public void ARevancheAcontece()
    {
        ProcurarRevanche().Should().NotBeNull(
            "uma carreira de derrotas precisa produzir alguém com conta a acertar");
    }

    [Fact]
    public void ORivalVoltaComOsMesmosAtributosComQueVenceu()
    {
        var (carreira, revanche) = ProcurarRevanche()!.Value;
        var rival = carreira.Rivais.Single(candidato => candidato.Id == revanche.RivalId);

        revanche.AtributosDoAdversario.Listar().Should().BeEquivalentTo(rival.Atributos.Listar(),
            "o reencontro mede o que o jogador construiu, não o balanceamento do gerador");

        revanche.Adversario.Should().Be(rival.Nome);
        revanche.VitoriasDoAdversarioSobreVoce.Should().BeGreaterThan(0);
        revanche.Chamada.Should().StartWith("Revanche");
    }

    /// <summary>
    /// Joga carreiras de lutadores fracos até uma revanche aparecer na mesa.
    /// Fracos de propósito: quem perde é quem gera alguém com conta a acertar.
    /// </summary>
    private static (Carreira Carreira, OfertaDeLuta Revanche)? ProcurarRevanche()
    {
        foreach (var semente in Enumerable.Range(1, 30))
        {
            var (partida, carreira, motor) = CarreiraFicticia(semente);

            while (!carreira.Encerrada)
            {
                if (carreira.Ofertas.FirstOrDefault(oferta => oferta.EhRevanche) is { } revanche)
                {
                    return (carreira, revanche);
                }

                Jogar(partida, carreira, motor);
            }
        }

        return null;
    }

    private static void Jogar(Partida partida, Carreira carreira, MotorDeCarreira motor)
    {
        if (carreira.Estado.EstaLesionado)
        {
            motor.Recuperar(partida, carreira, RankingDoJogo.Vazio);
            return;
        }

        motor.Aceitar(partida, carreira, RankingDoJogo.Vazio, carreira.Ofertas[0].Indice);
    }

    private static (Partida, Carreira, MotorDeCarreira) CarreiraFicticia(int semente)
    {
        var motor = Cenario.Motor();
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(64), seed: semente);

        return (partida, motor.Iniciar(partida, RankingDoJogo.Vazio), motor);
    }
}
