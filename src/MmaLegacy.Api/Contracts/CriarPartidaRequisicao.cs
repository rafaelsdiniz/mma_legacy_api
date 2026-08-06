using System.ComponentModel.DataAnnotations;
using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// Dados da ficha de inscrição enviados para abrir uma partida.
/// </summary>
/// <param name="Nome">Nome do lutador.</param>
/// <param name="Apelido">Apelido que aparece entre aspas no cartaz.</param>
/// <param name="Nacionalidade">País que o lutador representa.</param>
/// <param name="CategoriaDePeso">Divisão em que vai competir.</param>
/// <param name="IdadeInicial">Idade de estreia profissional.</param>
/// <param name="BaseDeLuta">Arte marcial de origem.</param>
/// <param name="Seed">
/// Semente da partida. Deixe em branco para sortear. Informar a mesma semente
/// reproduz exatamente o mesmo draft e a mesma carreira.
/// </param>
public sealed record CriarPartidaRequisicao(
    [property: Required(ErrorMessage = "Informe o nome do lutador.")]
    [property: StringLength(FichaDeInscricao.TamanhoMaximoDoNome, MinimumLength = 2)]
    string Nome,

    [property: Required(ErrorMessage = "Informe o apelido do lutador.")]
    [property: StringLength(FichaDeInscricao.TamanhoMaximoDoApelido, MinimumLength = 1)]
    string Apelido,

    [property: Required(ErrorMessage = "Informe a nacionalidade.")]
    [property: StringLength(60, MinimumLength = 2)]
    string Nacionalidade,

    // Os enums são anuláveis de propósito: sem isso, uma requisição que
    // esquecesse o campo passaria pela validação com o valor 0 e o jogador
    // receberia uma categoria que não escolheu.
    [property: Required(ErrorMessage = "Escolha a categoria de peso.")]
    CategoriaDePeso? CategoriaDePeso,

    [property: Range(FichaDeInscricao.IdadeMinima, FichaDeInscricao.IdadeMaxima,
        ErrorMessage = "A idade de estreia deve estar entre {1} e {2} anos.")]
    int IdadeInicial,

    [property: Required(ErrorMessage = "Escolha a base de luta.")]
    BaseDeLuta? BaseDeLuta,

    int? Seed)
{
    /// <summary>Converte a requisição validada na ficha do domínio.</summary>
    public FichaDeInscricao ParaFicha() => new(
        Nome,
        Apelido,
        Nacionalidade,
        CategoriaDePeso!.Value,
        IdadeInicial,
        BaseDeLuta!.Value);
}
