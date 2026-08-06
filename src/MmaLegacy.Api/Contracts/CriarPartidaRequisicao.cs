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
/// <remarks>
/// Os atributos de validação ficam no <b>parâmetro</b> do construtor, sem o
/// alvo <c>property:</c>. Em records posicionais o MVC lê a metadata do
/// parâmetro e recusa a requisição inteira com <c>InvalidOperationException</c>
/// se encontrar validação presa à propriedade gerada.
/// </remarks>
public sealed record CriarPartidaRequisicao(
    // As mensagens são escritas para o jogador ler no formulário, não para o
    // desenvolvedor ler no log. Sem elas o ASP.NET devolve o texto padrão em
    // inglês e o front-end teria que traduzir por conta própria.
    [Required(ErrorMessage = "Informe o nome do lutador.")]
    [StringLength(FichaDeInscricao.TamanhoMaximoDoNome, MinimumLength = 2,
        ErrorMessage = "O nome deve ter entre {2} e {1} caracteres.")]
    string Nome,

    [Required(ErrorMessage = "Informe o apelido do lutador.")]
    [StringLength(FichaDeInscricao.TamanhoMaximoDoApelido, MinimumLength = 1,
        ErrorMessage = "O apelido deve ter no máximo {1} caracteres.")]
    string Apelido,

    [Required(ErrorMessage = "Informe a nacionalidade.")]
    [StringLength(60, MinimumLength = 2,
        ErrorMessage = "A nacionalidade deve ter entre {2} e {1} caracteres.")]
    string Nacionalidade,

    // Os enums são anuláveis de propósito: sem isso, uma requisição que
    // esquecesse o campo passaria pela validação com o valor 0 e o jogador
    // receberia uma categoria que não escolheu.
    [Required(ErrorMessage = "Escolha a categoria de peso.")]
    CategoriaDePeso? CategoriaDePeso,

    [Range(FichaDeInscricao.IdadeMinima, FichaDeInscricao.IdadeMaxima,
        ErrorMessage = "A idade de estreia deve estar entre {1} e {2} anos.")]
    int IdadeInicial,

    [Required(ErrorMessage = "Escolha a base de luta.")]
    BaseDeLuta? BaseDeLuta,

    int? Seed,

    /// <summary>
    /// Omitido, vale <see cref="Domain.Enums.NivelDeDificuldade.Facil"/>. É o
    /// padrão porque quem chega sem escolher está jogando pela primeira vez, e
    /// o modo difícil sem contexto nenhum é frustrante, não desafiador.
    /// </summary>
    NivelDeDificuldade? NivelDeDificuldade)
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
