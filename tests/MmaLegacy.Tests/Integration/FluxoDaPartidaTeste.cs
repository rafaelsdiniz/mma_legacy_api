using FluentAssertions;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Services;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Integration;

/// <summary>
/// O caminho completo de uma partida, passando pelos serviços e pelo banco:
/// criar, draftar oito vezes, simular a carreira e reler tudo.
/// </summary>
public sealed class FluxoDaPartidaTeste : IDisposable
{
    private readonly BancoDeTeste _banco = new();

    public FluxoDaPartidaTeste()
    {
        using var contexto = _banco.CriarContexto();
        AcervoDeLutadores.SemearAsync(contexto).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UmaPartidaVaiDoDraftAoVereditoDeLegado()
    {
        var partidaId = await CriarPartidaAsync(seed: 20260805);

        await CompletarDraftAsync(partidaId);
        await EstrearAsync(partidaId);
        await SimularORestoAsync(partidaId);

        // Contexto novo: obriga o EF a remontar o agregado a partir das tabelas.
        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).ObterAsync(partidaId);

        partida.Status.Should().Be(StatusDaPartida.CarreiraSimulada);
        partida.Rodadas.Should().HaveCount(Habilidades.Quantidade);
        partida.Rodadas.Should().OnlyContain(rodada => rodada.Concluida);

        var lutador = partida.ExigirLutadorMontado();
        lutador.Overall.Should().BeGreaterThan(0);
        lutador.Atributos.Listar().Should().HaveCount(Habilidades.Quantidade);

        var carreira = partida.ExigirCarreiraEncerrada();
        carreira.Lutas.Should().NotBeEmpty();
        carreira.Legado.Should().BeDefined();
        carreira.MotivoDoEncerramento.Should().NotBeNull();
        carreira.Ofertas.Should().BeEmpty();
        (carreira.Vitorias + carreira.Derrotas + carreira.Empates).Should().Be(carreira.TotalDeLutas);
    }

    [Fact]
    public async Task AEstreiaPoeOfertasNaMesaESobreviveARecarregarAPagina()
    {
        var partidaId = await CriarPartidaAsync(seed: 20260805);
        await CompletarDraftAsync(partidaId);

        var estreia = await EstrearAsync(partidaId);
        estreia.Ofertas.Should().NotBeEmpty();
        estreia.Encerrada.Should().BeFalse();

        // Contexto novo: as ofertas precisam voltar do banco iguais, com os
        // atributos do adversário intactos. É o que sustenta a carreira jogada
        // entre requisições.
        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).ObterAsync(partidaId);
        var carreira = partida.ExigirCarreira();

        partida.Status.Should().Be(StatusDaPartida.CarreiraEmAndamento);
        carreira.Ofertas.Should().HaveCount(estreia.Ofertas.Count);
        carreira.Ofertas[0].Adversario.Should().Be(estreia.Ofertas[0].Adversario);
        carreira.Ofertas[0].OverallDoAdversario.Should().Be(estreia.Ofertas[0].OverallDoAdversario);
        carreira.Estado.Idade.Should().Be(carreira.IdadeDeEstreia);
    }

    [Fact]
    public async Task EstrearDuasVezesNaoSorteiaOutraVidaParaOMesmoLutador()
    {
        var partidaId = await CriarPartidaAsync(seed: 4242);
        await CompletarDraftAsync(partidaId);

        var primeira = await EstrearAsync(partidaId);
        var segunda = await EstrearAsync(partidaId);

        segunda.Ofertas[0].Adversario.Should().Be(primeira.Ofertas[0].Adversario);

        await using var contexto = _banco.CriarContexto();
        contexto.Carreiras.Count().Should().Be(1);
    }

    [Fact]
    public async Task AceitarUmaOfertaGeraUmaLutaEUmaNovaRodadaDeOfertas()
    {
        var partidaId = await CriarPartidaAsync(seed: 77);
        await CompletarDraftAsync(partidaId);
        await EstrearAsync(partidaId);

        Carreira carreira;
        MmaLegacy.Api.Simulation.PassoDaCarreira passo;

        await using (var contexto = _banco.CriarContexto())
        {
            var jogada = await MontarServicoDeCarreira(contexto).AceitarAsync(partidaId, indiceDaOferta: 1);
            carreira = jogada.Partida.ExigirCarreira();
            passo = jogada.Passo;
        }

        passo.Luta.Should().NotBeNull();
        passo.Desfecho!.Rounds.Should().NotBeEmpty();
        carreira.TotalDeLutas.Should().Be(1);
        carreira.Ofertas.Should().NotBeEmpty();

        // A oferta aceita sai da mesa: a rodada seguinte é gente nova.
        carreira.Ofertas.Should().NotContain(oferta => oferta.Adversario == passo.Luta!.Adversario);
    }

    [Fact]
    public async Task RecusarTresRodadasSeguidasNoRegionalEncerraACarreiraSemContrato()
    {
        var partidaId = await CriarPartidaAsync(seed: 909);
        await CompletarDraftAsync(partidaId);
        await EstrearAsync(partidaId);

        for (var recusa = 0; recusa < 3; recusa++)
        {
            await using var contexto = _banco.CriarContexto();
            await MontarServicoDeCarreira(contexto).RecusarAsync(partidaId);
        }

        await using var leitura = _banco.CriarContexto();
        var carreira = (await new ServicoDePartida(leitura).ObterAsync(partidaId)).ExigirCarreira();

        carreira.Encerrada.Should().BeTrue();
        carreira.MotivoDoEncerramento.Should().Be(MotivoDoEncerramento.SemContrato);
        carreira.TotalDeLutas.Should().Be(0);
        carreira.Ofertas.Should().BeEmpty();
    }

    [Fact]
    public async Task NaoJogaCarreiraDeUmLutadorQueAindaNaoEstreou()
    {
        var partidaId = await CriarPartidaAsync(seed: 31);
        await CompletarDraftAsync(partidaId);

        await using var contexto = _banco.CriarContexto();

        var jogadaPrematura = async () =>
            await MontarServicoDeCarreira(contexto).AceitarAsync(partidaId, indiceDaOferta: 1);

        await jogadaPrematura.Should()
            .ThrowAsync<RegraDeNegocioException>()
            .WithMessage("*ainda não estreou*");
    }

    [Fact]
    public async Task ODraftSorteiaOitoAtletasDistintosDoAcervo()
    {
        var partidaId = await CriarPartidaAsync(seed: 42);

        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).ObterAsync(partidaId);

        partida.Rodadas.Should().HaveCount(Habilidades.Quantidade);
        partida.Rodadas.Select(rodada => rodada.LutadorId).Should().OnlyHaveUniqueItems();
        partida.Rodadas.Select(rodada => rodada.Ordem).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AMesmaSementeSorteiaOMesmoDraftEProduzOMesmoLutador()
    {
        var primeiraId = await CriarPartidaAsync(seed: 777);
        var segundaId = await CriarPartidaAsync(seed: 777);

        await CompletarDraftAsync(primeiraId);
        await CompletarDraftAsync(segundaId);

        await using var contexto = _banco.CriarContexto();
        var servico = new ServicoDePartida(contexto);
        var primeira = await servico.ObterAsync(primeiraId);
        var segunda = await servico.ObterAsync(segundaId);

        segunda.Rodadas.Select(rodada => rodada.LutadorNome)
            .Should().Equal(primeira.Rodadas.Select(rodada => rodada.LutadorNome));

        segunda.ExigirLutadorMontado().Overall.Should().Be(primeira.ExigirLutadorMontado().Overall);
    }

    [Fact]
    public async Task AMesmaSementeJogaAMesmaCarreiraDoComecoAoFim()
    {
        var primeiraId = await CriarPartidaAsync(seed: 99);
        var segundaId = await CriarPartidaAsync(seed: 99);

        await CompletarDraftAsync(primeiraId);
        await CompletarDraftAsync(segundaId);

        var primeira = await JogarCarreiraInteiraAsync(primeiraId);
        var segunda = await JogarCarreiraInteiraAsync(segundaId);

        segunda.Cartel.Should().Be(primeira.Cartel);
        segunda.IdadeDeAposentadoria.Should().Be(primeira.IdadeDeAposentadoria);
        segunda.MotivoDoEncerramento.Should().Be(primeira.MotivoDoEncerramento);
        segunda.Lutas.Select(luta => luta.Adversario)
            .Should().Equal(primeira.Lutas.Select(luta => luta.Adversario));
    }

    [Fact]
    public async Task NaoEstreiaCarreiraComODraftPelaMetade()
    {
        var partidaId = await CriarPartidaAsync(seed: 5);

        await using var contexto = _banco.CriarContexto();
        var servico = MontarServicoDeCarreira(contexto);

        var estreiaPrematura = async () => await servico.EstrearAsync(partidaId);

        await estreiaPrematura.Should()
            .ThrowAsync<RegraDeNegocioException>()
            .WithMessage("*faltam 8 escolha*");
    }

    [Fact]
    public async Task RecusaEscolhaDeHabilidadeJaOcupadaAtravessandoOsServicos()
    {
        var partidaId = await CriarPartidaAsync(seed: 11);

        await using var contexto = _banco.CriarContexto();
        var servicoDeDraft = new ServicoDeDraft(contexto, new ServicoDePartida(contexto));

        var primeira = await servicoDeDraft.ObterRodadaAtualAsync(partidaId);
        await servicoDeDraft.EscolherAsync(partidaId, primeira.Atleta.Id, Habilidade.Striking);

        var segunda = await servicoDeDraft.ObterRodadaAtualAsync(partidaId);
        var escolhaRepetida = async () =>
            await servicoDeDraft.EscolherAsync(partidaId, segunda.Atleta.Id, Habilidade.Striking);

        await escolhaRepetida.Should().ThrowAsync<RegraDeNegocioException>();
    }

    [Fact]
    public async Task PartidaInexistenteVira404()
    {
        await using var contexto = _banco.CriarContexto();

        var consultaPerdida = async () => await new ServicoDePartida(contexto).ObterAsync(Guid.NewGuid());

        await consultaPerdida.Should().ThrowAsync<RecursoNaoEncontradoException>();
    }

    [Fact]
    public async Task OSeedDoAcervoEIdempotente()
    {
        await using var contexto = _banco.CriarContexto();
        var antes = contexto.Lutadores.Count();

        await AcervoDeLutadores.SemearAsync(contexto);

        contexto.Lutadores.Count().Should().Be(antes);
    }

    private async Task<Guid> CriarPartidaAsync(int seed)
    {
        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).CriarAsync(Cenario.Ficha(), seed);

        return partida.Id;
    }

    /// <summary>
    /// Percorre as oito rodadas escolhendo sempre a primeira habilidade ainda
    /// disponível — é o que um jogador faria, do ponto de vista da API.
    /// </summary>
    private async Task CompletarDraftAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();
        var servicoDeDraft = new ServicoDeDraft(contexto, new ServicoDePartida(contexto));

        for (var rodada = 0; rodada < Habilidades.Quantidade; rodada++)
        {
            var atual = await servicoDeDraft.ObterRodadaAtualAsync(partidaId);
            var habilidade = atual.Partida.HabilidadesDisponiveis()[0];

            await servicoDeDraft.EscolherAsync(partidaId, atual.Atleta.Id, habilidade);
        }
    }

    private async Task<Carreira> EstrearAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();

        return (await MontarServicoDeCarreira(contexto).EstrearAsync(partidaId)).ExigirCarreira();
    }

    private async Task<Carreira> SimularORestoAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();
        var jogada = await MontarServicoDeCarreira(contexto).SimularORestoAsync(partidaId);

        return jogada.Partida.ExigirCarreira();
    }

    private async Task<Carreira> JogarCarreiraInteiraAsync(Guid partidaId)
    {
        await EstrearAsync(partidaId);

        return await SimularORestoAsync(partidaId);
    }

    private static ServicoDeCarreira MontarServicoDeCarreira(ContextoDoJogo contexto) => new(
        contexto,
        new ServicoDePartida(contexto),
        new MotorDeCarreira(new MotorDeLuta(), new GeradorDeAdversarios()));

    public void Dispose() => _banco.Dispose();
}
