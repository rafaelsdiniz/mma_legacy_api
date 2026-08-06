using FluentAssertions;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Tests.Domain;

/// <summary>
/// Protege a lista escrita à mão de quem entra no draft.
/// </summary>
/// <remarks>
/// O elenco casa por slug, e slug que não bate com ninguém não dá erro: o
/// atleta simplesmente nunca aparece. É o tipo de falha que ninguém percebe
/// jogando, porque o draft continua funcionando — só que sem o Khabib.
/// </remarks>
public sealed class ElencoDoDraftTeste
{
    private static readonly IReadOnlyList<Lutador> Acervo = AcervoDeLutadores.Montar();

    [Fact]
    public void TodoNomeDoElencoExisteNoAcervo()
    {
        var slugsDoAcervo = Acervo.Select(atleta => atleta.Slug).ToHashSet();

        var semCorrespondente = ElencoDoDraft.NomesEscritos
            .Where(nome => !slugsDoAcervo.Contains(Lutador.GerarSlug(nome)))
            .ToList();

        semCorrespondente.Should().BeEmpty(
            "todo nome do elenco do draft precisa bater com um atleta do acervo");
    }

    [Fact]
    public void OElencoHabilitaExatamenteOsAtletasDaLista()
    {
        var sorteaveis = Acervo.Where(atleta => atleta.SorteavelNoDraft).ToList();

        sorteaveis.Should().HaveCount(ElencoDoDraft.Quantidade);
        sorteaveis.Should().OnlyContain(atleta => ElencoDoDraft.Contem(atleta));
    }

    [Fact]
    public void OElencoTemAtletasSuficientesParaUmDraftInteiro()
    {
        // Oito rodadas mais os pulos: um elenco apertado faria o mesmo atleta
        // reaparecer partida após partida.
        Acervo.Count(atleta => atleta.SorteavelNoDraft)
            .Should().BeGreaterThan(Habilidades.Quantidade * 3);
    }

    [Fact]
    public void TirarAlguemDoDraftNaoOTiraDoRanking()
    {
        // Robert Whittaker é o caso que motivou a separação: nome grande demais
        // para sumir do jogo, específico demais para o draft de quem entrou hoje.
        var foraDoDraft = Acervo
            .Where(atleta => !atleta.SorteavelNoDraft && atleta.EstaRanqueado)
            .ToList();

        foraDoDraft.Should().NotBeEmpty();
        foraDoDraft.Should().OnlyContain(atleta => atleta.PosicaoNoRanking >= 0);
    }
}
