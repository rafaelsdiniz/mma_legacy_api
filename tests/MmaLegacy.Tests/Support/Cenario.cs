using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Tests.Support;

/// <summary>
/// Fábricas de dados para os testes. Concentrar a montagem aqui evita que cada
/// teste repita oito notas e um draft inteiro só para chegar ao que quer medir.
/// </summary>
public static class Cenario
{
    /// <summary>
    /// Atributos com uma nota padrão em tudo, sobrescrevendo apenas o que o
    /// teste precisa destacar.
    /// </summary>
    public static Atributos Atributos(
        int padrao = 80,
        int? striking = null,
        int? potencia = null,
        int? velocidade = null,
        int? wrestling = null,
        int? jiuJitsu = null,
        int? cardio = null,
        int? resistencia = null,
        int? inteligenciaDeLuta = null) =>
        new(
            striking ?? padrao,
            potencia ?? padrao,
            velocidade ?? padrao,
            wrestling ?? padrao,
            jiuJitsu ?? padrao,
            cardio ?? padrao,
            resistencia ?? padrao,
            inteligenciaDeLuta ?? padrao);

    public static FichaDeInscricao Ficha(
        CategoriaDePeso categoria = CategoriaDePeso.MeioPesado,
        int idadeInicial = 22) =>
        new("Rafael Diniz", "The Machine", "Brasil", categoria, idadeInicial, BaseDeLuta.MuayThai);

    /// <summary>
    /// Oito atletas distintos com as mesmas notas.
    /// </summary>
    /// <remarks>
    /// Como o draft apresenta um atleta por rodada e todos têm as mesmas notas,
    /// escolher a i-ésima habilidade do i-ésimo atleta produz um lutador com
    /// exatamente os atributos informados — o que permite testar overall,
    /// estilo e carreira a partir de um insumo controlado.
    /// </remarks>
    public static IReadOnlyList<Lutador> Acervo(Atributos notas) =>
        Enumerable.Range(1, Habilidades.Quantidade)
            .Select(numero => new Lutador($"Atleta de Teste {numero}", "Brasil", notas))
            .ToList();

    /// <summary>Uma partida com o draft já finalizado e o lutador montado.</summary>
    public static Partida PartidaComDraftConcluido(
        Atributos? atributos = null,
        int seed = 20260805,
        FichaDeInscricao? ficha = null)
    {
        var notas = atributos ?? Atributos();
        var acervo = Acervo(notas);
        var partida = Partida.Iniciar(ficha ?? Ficha(), seed, acervo);

        for (var rodada = 0; rodada < Habilidades.Quantidade; rodada++)
        {
            partida.EscolherHabilidade(acervo[rodada], Habilidades.Todas[rodada]);
        }

        return partida;
    }
}
