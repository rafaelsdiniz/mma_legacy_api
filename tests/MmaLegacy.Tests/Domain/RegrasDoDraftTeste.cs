using FluentAssertions;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Domain;

/// <summary>
/// As regras que impedem o jogador de burlar o draft. São a fronteira de
/// confiança da aplicação: o front-end pode mandar qualquer coisa.
/// </summary>
public sealed class RegrasDoDraftTeste
{
    [Fact]
    public void NaoPermiteSelecionarHabilidadeJaOcupada()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        partida.EscolherHabilidade(acervo[0], Habilidade.Striking);

        var escolhaRepetida = () => partida.EscolherHabilidade(acervo[1], Habilidade.Striking);

        escolhaRepetida.Should()
            .Throw<RegraDeNegocioException>()
            .WithMessage("*Striking*já foi preenchida*");
    }

    [Fact]
    public void NaoPermiteUsarAtletaForaDaRodadaAtual()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        var atletaDaRodadaSeguinte = () => partida.EscolherHabilidade(acervo[3], Habilidade.Potencia);

        atletaDaRodadaSeguinte.Should()
            .Throw<RegraDeNegocioException>()
            .WithMessage("*não é o atleta da rodada 1*");
    }

    [Fact]
    public void NaoPermiteUsarAtletaQueNaoEstaNoDraft()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);
        var intruso = new Lutador("Atleta Que Nao Foi Sorteado", "Brasil", Cenario.Atributos());

        var escolhaComIntruso = () => partida.EscolherHabilidade(intruso, Habilidade.Cardio);

        escolhaComIntruso.Should().Throw<RegraDeNegocioException>();
    }

    [Fact]
    public void MontaOLutadorSomenteAposAsOitoEscolhas()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        for (var rodada = 0; rodada < Habilidades.Quantidade - 1; rodada++)
        {
            partida.EscolherHabilidade(acervo[rodada], Habilidades.Todas[rodada]);

            partida.Status.Should().Be(StatusDaPartida.DraftEmAndamento);
            partida.Lutador.Should().BeNull();
        }

        partida.EscolherHabilidade(acervo[^1], Habilidades.Todas[^1]);

        partida.Status.Should().Be(StatusDaPartida.DraftConcluido);
        partida.Lutador.Should().NotBeNull();
        partida.EscolhasFeitas.Should().Be(Habilidades.Quantidade);
    }

    [Fact]
    public void GravaANotaVindaDoAcervoEIgnoraQualquerValorDoCliente()
    {
        // O contrato da API não tem campo de nota justamente por isto: a única
        // fonte possível é o atributo cadastrado no atleta.
        var acervo = Cenario.Acervo(Cenario.Atributos(padrao: 70, potencia: 99));
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        partida.EscolherHabilidade(acervo[0], Habilidade.Potencia);

        partida.Rodadas[0].NotaObtida.Should().Be(99);
    }

    [Fact]
    public void RecusaDraftComQuantidadeDeAtletasDiferenteDeOito()
    {
        var acervoIncompleto = Cenario.Acervo(Cenario.Atributos()).Take(5).ToList();

        var inicioInvalido = () => Partida.Iniciar(Cenario.Ficha(), seed: 1, acervoIncompleto);

        inicioInvalido.Should().Throw<RegraDeNegocioException>().WithMessage("*exatamente 8 atletas*");
    }

    [Fact]
    public void RecusaDraftComAtletaRepetido()
    {
        var atleta = new Lutador("Atleta Repetido", "Brasil", Cenario.Atributos());
        var acervoComRepetido = Enumerable.Repeat(atleta, Habilidades.Quantidade).ToList();

        var inicioInvalido = () => Partida.Iniciar(Cenario.Ficha(), seed: 1, acervoComRepetido);

        inicioInvalido.Should().Throw<RegraDeNegocioException>().WithMessage("*duas vezes*");
    }

    [Fact]
    public void NaoPermiteEscolherDepoisDoDraftConcluido()
    {
        var partida = Cenario.PartidaComDraftConcluido();
        var atleta = new Lutador("Atleta Atrasado", "Brasil", Cenario.Atributos());

        var escolhaTardia = () => partida.EscolherHabilidade(atleta, Habilidade.Cardio);

        escolhaTardia.Should().Throw<RegraDeNegocioException>().WithMessage("*já foi concluído*");
    }

    [Fact]
    public void NaoPermiteSimularCarreiraAntesDeTerminarODraft()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        var lutadorPrematuro = () => partida.ExigirLutadorMontado();

        lutadorPrematuro.Should().Throw<RegraDeNegocioException>().WithMessage("*faltam 8 escolha*");
    }

    [Fact]
    public void RemoveAHabilidadeEscolhidaDaListaDeDisponiveis()
    {
        var acervo = Cenario.Acervo(Cenario.Atributos());
        var partida = Partida.Iniciar(Cenario.Ficha(), seed: 1, acervo);

        partida.EscolherHabilidade(acervo[0], Habilidade.Wrestling);

        partida.HabilidadesDisponiveis().Should()
            .HaveCount(Habilidades.Quantidade - 1)
            .And.NotContain(Habilidade.Wrestling);
    }
}
