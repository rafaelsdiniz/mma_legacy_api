using FluentAssertions;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Domain;

/// <summary>
/// A tabela do ranking com o jogador encaixado nela.
/// </summary>
/// <remarks>
/// O ranking oficial do acervo nunca muda: a subida do jogador é privada da
/// partida dele. Estes testes travam essa promessa, que é o que permite dois
/// jogadores serem campeões da mesma divisão ao mesmo tempo sem um enxergar o
/// outro.
/// </remarks>
public sealed class RankingDaCarreiraTeste
{
    private static readonly RankingDoJogo Ranking = new(AcervoDeLutadores.Montar());

    private static TabelaDaDivisao MeioPesado => Ranking.Da(CategoriaDePeso.MeioPesado);

    [Fact]
    public void ADivisaoTemCampeaoEQuinzeRanqueados()
    {
        MeioPesado.EstaVazia.Should().BeFalse();
        MeioPesado.Em(TabelaDaDivisao.PosicaoDoCampeao).Should().NotBeNull();
        MeioPesado.Em(TabelaDaDivisao.UltimaPosicao).Should().NotBeNull();
    }

    [Fact]
    public void SemOJogadorATabelaEODoAcervoIntacta()
    {
        var tabela = MeioPesado.ComOJogador("Rafael Diniz", posicaoDoJogador: null);

        tabela.Should().HaveCount(TabelaDaDivisao.UltimaPosicao + 1);
        tabela.Should().NotContain(linha => linha.EhOJogador);
        tabela.Select(linha => linha.Posicao).Should().BeInAscendingOrder();
    }

    [Fact]
    public void OJogadorEncaixadoEmpurraTodoMundoAbaixoDeleUmDegrau()
    {
        var original = MeioPesado.ComOJogador("Rafael Diniz", null);
        var comJogador = MeioPesado.ComOJogador("Rafael Diniz", posicaoDoJogador: 8);

        comJogador.Single(linha => linha.EhOJogador).Posicao.Should().Be(8);

        // Quem estava acima não se mexe.
        comJogador[7].Nome.Should().Be(original[7].Nome);

        // Quem estava na vaga tomada desce exatamente um degrau.
        comJogador[9].Nome.Should().Be(original[8].Nome);

        // A tabela não cresce: o antigo décimo quinto cai fora do ranking.
        comJogador.Should().HaveCount(TabelaDaDivisao.UltimaPosicao + 1);
        comJogador.Select(linha => linha.Nome).Should().NotContain(original[^1].Nome);
    }

    [Fact]
    public void OJogadorCampeaoEmpurraOCampeaoRealParaOPrimeiroLugar()
    {
        var original = MeioPesado.ComOJogador("Rafael Diniz", null);
        var comJogador = MeioPesado.ComOJogador(
            "Rafael Diniz",
            TabelaDaDivisao.PosicaoDoCampeao);

        comJogador[0].EhOJogador.Should().BeTrue();
        comJogador[1].Nome.Should().Be(original[0].Nome);
    }

    [Fact]
    public void QuemEstaForaDoRankingEnfrentaAParteDeBaixoDaTabela()
    {
        var alvos = MeioPesado.AlvosDe(posicaoDoJogador: null, quantidade: 2);

        alvos.Should().NotBeEmpty();
        alvos.Should().OnlyContain(alvo => alvo >= 12);
    }

    [Fact]
    public void QuemJaEstaRanqueadoSoDesafiaParaCima()
    {
        var alvos = MeioPesado.AlvosDe(posicaoDoJogador: 8, quantidade: 2);

        alvos.Should().NotBeEmpty();
        alvos.Should().OnlyContain(alvo => alvo < 8 && alvo > TabelaDaDivisao.PosicaoDoCampeao);
    }

    [Fact]
    public void OAcervoNuncaEAlteradoPelaSubidaDoJogador()
    {
        var antes = AcervoDeLutadores.Montar()
            .Where(atleta => atleta.Categoria == CategoriaDePeso.MeioPesado)
            .ToDictionary(atleta => atleta.Slug, atleta => atleta.PosicaoNoRanking);

        // Simula o jogador chegando a campeão e a tabela sendo lida várias vezes.
        MeioPesado.ComOJogador("Rafael Diniz", TabelaDaDivisao.PosicaoDoCampeao);
        MeioPesado.ComOJogador("Rafael Diniz", 3);

        var depois = AcervoDeLutadores.Montar()
            .Where(atleta => atleta.Categoria == CategoriaDePeso.MeioPesado)
            .ToDictionary(atleta => atleta.Slug, atleta => atleta.PosicaoNoRanking);

        depois.Should().BeEquivalentTo(antes);
    }

    [Fact]
    public void NoUfcOsAdversariosSaoAtletasReaisDoRanking()
    {
        var motor = Cenario.Motor();
        var partida = Cenario.PartidaComDraftConcluido(Cenario.Atributos(96), seed: 42);
        var carreira = motor.Simular(partida, Ranking);

        var nomesDoAcervo = AcervoDeLutadores.Montar()
            .Where(atleta => atleta.EstaRanqueado)
            .Select(atleta => atleta.Nome)
            .ToHashSet();

        var noUfc = carreira.Lutas
            .Where(luta => luta.Organizacao == NivelDaOrganizacao.GrandeOrganizacao)
            .ToList();

        noUfc.Should().NotBeEmpty("um build 96 chega ao UFC");
        noUfc.Should().Contain(luta => nomesDoAcervo.Contains(luta.Adversario));
    }
}
