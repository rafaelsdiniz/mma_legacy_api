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
        await SimularCarreiraAsync(partidaId);

        // Contexto novo: obriga o EF a remontar o agregado a partir das tabelas.
        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).ObterAsync(partidaId);

        partida.Status.Should().Be(StatusDaPartida.CarreiraSimulada);
        partida.Rodadas.Should().HaveCount(Habilidades.Quantidade);
        partida.Rodadas.Should().OnlyContain(rodada => rodada.Concluida);

        var lutador = partida.ExigirLutadorMontado();
        lutador.Overall.Should().BeGreaterThan(0);
        lutador.Atributos.Listar().Should().HaveCount(Habilidades.Quantidade);

        var carreira = partida.ExigirCarreiraSimulada();
        carreira.Lutas.Should().NotBeEmpty();
        carreira.Legado.Should().BeDefined();
        (carreira.Vitorias + carreira.Derrotas + carreira.Empates).Should().Be(carreira.TotalDeLutas);
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
    public async Task SimularDuasVezesDevolveAMesmaCarreiraSemDuplicar()
    {
        var partidaId = await CriarPartidaAsync(seed: 99);
        await CompletarDraftAsync(partidaId);

        var primeira = await SimularCarreiraAsync(partidaId);
        var segunda = await SimularCarreiraAsync(partidaId);

        segunda.Id.Should().Be(primeira.Id);
        segunda.Cartel.Should().Be(primeira.Cartel);

        await using var contexto = _banco.CriarContexto();
        contexto.Carreiras.Count().Should().Be(1);
    }

    [Fact]
    public async Task NaoSimulaCarreiraComODraftPelaMetade()
    {
        var partidaId = await CriarPartidaAsync(seed: 5);

        await using var contexto = _banco.CriarContexto();
        var servico = MontarServicoDeCarreira(contexto);

        var simulacaoPrematura = async () => await servico.SimularAsync(partidaId);

        await simulacaoPrematura.Should()
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

    private async Task<Carreira> SimularCarreiraAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();

        return await MontarServicoDeCarreira(contexto).SimularAsync(partidaId);
    }

    private static ServicoDeCarreira MontarServicoDeCarreira(ContextoDoJogo contexto) => new(
        contexto,
        new ServicoDePartida(contexto),
        new MotorDeCarreira(new MotorDeLuta(), new GeradorDeAdversarios()));

    public void Dispose() => _banco.Dispose();
}
