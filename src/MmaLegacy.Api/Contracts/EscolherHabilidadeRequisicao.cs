using System.ComponentModel.DataAnnotations;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// A decisão do jogador em uma rodada do draft.
/// </summary>
/// <remarks>
/// Repare no que <b>não</b> existe aqui: nenhum campo de nota. O cliente diz
/// apenas de quem quer e o quê; o valor sai do acervo no servidor. É por isso
/// que alterar números pelas ferramentas do navegador não muda o lutador
/// montado.
/// </remarks>
/// <param name="AtletaId">O atleta apresentado na rodada atual.</param>
/// <param name="Habilidade">A habilidade que o jogador quer levar dele.</param>
public sealed record EscolherHabilidadeRequisicao(
    [property: Required(ErrorMessage = "Informe o atleta da rodada.")]
    Guid AtletaId,

    [property: Required(ErrorMessage = "Escolha uma habilidade.")]
    Habilidade? Habilidade);
