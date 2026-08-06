using FluentAssertions;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Services;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Integration;

/// <summary>
/// O pulo do draft, atravessando serviço e banco.
/// </summary>
/// <remarks>
/// A regra existia no domínio desde cedo, mas sem endpoint nem tela — ninguém
/// conseguia chegar nela, e por isso nada quebrava quando ela estava errada.
/// Estes testes fecham o caminho inteiro.
/// </remarks>
public sealed class PuloNoDraftTeste : IDisposable
{
    private readonly BancoDeTeste _banco = new();

    public PuloNoDraftTeste()
    {
        using var contexto = _banco.CriarContexto();
        AcervoDeLutadores.SemearAsync(contexto).GetAwaiter().GetResult();
    }

    [Theory]
    [InlineData(NivelDeDificuldade.Facil, 2)]
    [InlineData(NivelDeDificuldade.Dificil, 1)]
    public async Task CadaNivelTemASuaCotaDePulos(NivelDeDificuldade nivel, int cota)
    {
        var partidaId = await CriarPartidaAsync(nivel);

        for (var pulo = 0; pulo < cota; pulo++)
        {
            await PularAsync(partidaId);
        }

        var alemDaCota = async () => await PularAsync(partidaId);

        await alemDaCota.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*pulo*");
    }

    [Fact]
    public async Task PularTrocaOAtletaDaRodadaSemAvancarODraft()
    {
        var partidaId = await CriarPartidaAsync(NivelDeDificuldade.Facil);

        var antes = await ObterRodadaAsync(partidaId);
        await PularAsync(partidaId);
        var depois = await ObterRodadaAsync(partidaId);

        depois.Atleta.Id.Should().NotBe(antes.Atleta.Id);

        // O pulo gasta um pulo, não uma rodada: continua sendo a vez de escolher
        // a primeira habilidade.
        depois.Rodada.Ordem.Should().Be(antes.Rodada.Ordem);
        depois.Partida.EscolhasFeitas.Should().Be(0);
        depois.Partida.PulosRestantes.Should().Be(antes.Partida.PulosRestantes - 1);
    }

    [Fact]
    public async Task OSubstitutoNuncaERepetidoNoMesmoDraft()
    {
        var partidaId = await CriarPartidaAsync(NivelDeDificuldade.Facil);

        await PularAsync(partidaId);
        await PularAsync(partidaId);

        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto).ObterAsync(partidaId);

        partida.Rodadas.Select(rodada => rodada.LutadorId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task OSubstitutoSaiDoElencoDoDraftENaoDoAcervoInteiro()
    {
        var partidaId = await CriarPartidaAsync(NivelDeDificuldade.Facil);

        await PularAsync(partidaId);
        var rodada = await ObterRodadaAsync(partidaId);

        rodada.Atleta.SorteavelNoDraft.Should().BeTrue();
    }

    private async Task<Guid> CriarPartidaAsync(NivelDeDificuldade nivel)
    {
        await using var contexto = _banco.CriarContexto();
        var partida = await new ServicoDePartida(contexto)
            .CriarAsync(Cenario.Ficha(), seed: 20260806, nivelDeDificuldade: nivel);

        return partida.Id;
    }

    private async Task PularAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();
        await MontarServico(contexto).PularAsync(partidaId);
    }

    private async Task<ServicoDeDraft.RodadaEmAberto> ObterRodadaAsync(Guid partidaId)
    {
        await using var contexto = _banco.CriarContexto();

        return await MontarServico(contexto).ObterRodadaAtualAsync(partidaId);
    }

    private static ServicoDeDraft MontarServico(ContextoDoJogo contexto) =>
        new(contexto, new ServicoDePartida(contexto));

    public void Dispose() => _banco.Dispose();
}
