using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Um atleta do acervo — a referência real que aparece no draft e da qual o
/// jogador rouba uma habilidade. Não é o lutador montado pelo jogador; esse é o
/// <see cref="LutadorCriado"/>.
/// </summary>
/// <remarks>
/// As notas aqui são estimativas editoriais do jogo e vivem exclusivamente no
/// servidor. O front-end recebe as notas para exibir, mas nunca as envia de
/// volta: na hora de registrar a escolha, o valor gravado é sempre o que está
/// aqui.
/// </remarks>
public sealed class Lutador
{
    public Guid Id { get; private set; }

    /// <summary>Nome de exibição, com acentuação.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Versão normalizada do nome, usada para achar a imagem do atleta.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>País de origem, para exibir a bandeira no card.</summary>
    public string Pais { get; private set; } = string.Empty;

    public Atributos Atributos { get; private set; } = null!;

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Lutador()
    {
    }

    public Lutador(string nome, string pais, Atributos atributos)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        Nome = DadoInvalidoException.ExigirTextoPreenchido(nome, nameof(nome));
        Pais = DadoInvalidoException.ExigirTextoPreenchido(pais, nameof(pais));
        Slug = GerarSlug(Nome);
        Id = GerarIdDeterministico(Slug);
        Atributos = atributos;
    }

    /// <summary>
    /// Converte "Alex Poatan" em "alex-poatan": sem acento, sem maiúscula e sem
    /// espaço, para servir de nome de arquivo de imagem no front-end.
    /// </summary>
    public static string GerarSlug(string nome)
    {
        var semAcento = nome.Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        var construtor = new StringBuilder(semAcento.Length);
        foreach (var caractere in new string(semAcento).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(caractere))
            {
                construtor.Append(caractere);
            }
            else if (construtor.Length > 0 && construtor[^1] != '-')
            {
                construtor.Append('-');
            }
        }

        return construtor.ToString().Trim('-');
    }

    /// <summary>
    /// Deriva o Id do slug em vez de sortear um Guid novo.
    /// </summary>
    /// <remarks>
    /// Assim o mesmo atleta tem sempre o mesmo Id em qualquer banco, e rodar o
    /// seed duas vezes atualiza os registros em vez de duplicá-los. Sem isso,
    /// um draft diário salvo hoje apontaria para Ids inexistentes depois de uma
    /// recriação do banco.
    /// </remarks>
    public static Guid GerarIdDeterministico(string slug)
    {
        var digest = MD5.HashData(Encoding.UTF8.GetBytes(slug));
        return new Guid(digest);
    }
}
