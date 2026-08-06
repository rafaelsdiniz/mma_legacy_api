using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// Um atleta do acervo, como aparece na página que lista todos.
/// </summary>
/// <remarks>
/// Traz overall e estilo já calculados. São derivados das notas e o front-end
/// poderia recomputar, mas aí existiriam duas implementações da mesma regra —
/// e a do cliente sairia do ar na primeira vez que os pesos mudassem.
/// </remarks>
public sealed record LutadorDoAcervoResposta(
    Guid Id,
    string Nome,
    string Slug,
    string Pais,
    bool EhLenda,
    decimal Overall,
    EstiloDeLuta Estilo,
    string MaiorQualidade,
    string PrincipalFraqueza,
    IReadOnlyList<NotaDeHabilidadeResposta> Notas)
{
    public static LutadorDoAcervoResposta DeDominio(Lutador atleta) => new(
        atleta.Id,
        atleta.Nome,
        atleta.Slug,
        atleta.Pais,
        atleta.EhLenda,
        CalculadoraDeOverall.Calcular(atleta.Atributos),
        IdentificadorDeEstilo.Identificar(atleta.Atributos),
        Habilidades.NomeDeExibicao(atleta.Atributos.MaiorHabilidade()),
        Habilidades.NomeDeExibicao(atleta.Atributos.MenorHabilidade()),
        atleta.Atributos.Listar().Select(NotaDeHabilidadeResposta.DeDominio).ToList());
}
