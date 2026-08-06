using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// O que o jogador informa antes do draft começar: quem é o lutador, onde ele
/// compete e com que idade estreia. São dados de identidade — nenhum deles vira
/// atributo, mas a idade e a categoria dirigem a simulação da carreira.
/// </summary>
public sealed class FichaDeInscricao
{
    /// <summary>Idade mínima de estreia profissional aceita pelo jogo.</summary>
    public const int IdadeMinima = 18;

    /// <summary>
    /// Acima disso não sobra carreira para simular: o declínio começa aos 36 e
    /// a aposentadoria costuma chegar antes dos 40.
    /// </summary>
    public const int IdadeMaxima = 35;

    public const int TamanhoMaximoDoNome = 40;
    public const int TamanhoMaximoDoApelido = 30;

    public string Nome { get; private set; } = string.Empty;
    public string Apelido { get; private set; } = string.Empty;
    public string Nacionalidade { get; private set; } = string.Empty;
    public CategoriaDePeso CategoriaDePeso { get; private set; }
    public int IdadeInicial { get; private set; }
    public BaseDeLuta BaseDeLuta { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private FichaDeInscricao()
    {
    }

    public FichaDeInscricao(
        string nome,
        string apelido,
        string nacionalidade,
        CategoriaDePeso categoriaDePeso,
        int idadeInicial,
        BaseDeLuta baseDeLuta)
    {
        Nome = LimitarTamanho(
            DadoInvalidoException.ExigirTextoPreenchido(nome, nameof(nome)),
            TamanhoMaximoDoNome,
            nameof(nome));

        Apelido = LimitarTamanho(
            DadoInvalidoException.ExigirTextoPreenchido(apelido, nameof(apelido)),
            TamanhoMaximoDoApelido,
            nameof(apelido));

        Nacionalidade = DadoInvalidoException.ExigirTextoPreenchido(nacionalidade, nameof(nacionalidade));
        CategoriaDePeso = DadoInvalidoException.ExigirEnumValido(categoriaDePeso, nameof(categoriaDePeso));
        IdadeInicial = DadoInvalidoException.ExigirIntervalo(idadeInicial, IdadeMinima, IdadeMaxima, nameof(idadeInicial));
        BaseDeLuta = DadoInvalidoException.ExigirEnumValido(baseDeLuta, nameof(baseDeLuta));
    }

    /// <summary>Nome completo no formato usado no card: NOME "APELIDO" SOBRENOME.</summary>
    public string NomeDeCartaz()
    {
        var partes = Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length < 2
            ? $"{Nome} \"{Apelido}\""
            : $"{string.Join(' ', partes[..^1])} \"{Apelido}\" {partes[^1]}";
    }

    private static string LimitarTamanho(string valor, int tamanhoMaximo, string nomeDoCampo)
    {
        if (valor.Length > tamanhoMaximo)
        {
            throw new DadoInvalidoException(
                $"O campo '{nomeDoCampo}' deve ter no máximo {tamanhoMaximo} caracteres.");
        }

        return valor;
    }
}
