namespace MmaLegacy.Api.Domain.Exceptions;

/// <summary>
/// Um valor recebido não respeita as invariantes do domínio — nota fora da
/// escala, idade impossível, nome vazio. Vira <c>400 Bad Request</c>.
/// </summary>
/// <remarks>
/// É a última linha de defesa, não a primeira: a validação amigável acontece
/// nos contratos de entrada. Se uma destas exceções chega ao cliente, é sinal
/// de que faltou uma anotação de validação em algum contrato.
/// </remarks>
public sealed class DadoInvalidoException : DominioException
{
    public DadoInvalidoException(string mensagem) : base(mensagem)
    {
    }

    /// <summary>Garante que o texto tenha conteúdo, devolvendo-o sem espaços nas pontas.</summary>
    public static string ExigirTextoPreenchido(string? valor, string nomeDoCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DadoInvalidoException($"O campo '{nomeDoCampo}' é obrigatório.");
        }

        return valor.Trim();
    }

    /// <summary>Garante que o número esteja dentro do intervalo fechado informado.</summary>
    public static int ExigirIntervalo(int valor, int minimo, int maximo, string nomeDoCampo)
    {
        if (valor < minimo || valor > maximo)
        {
            throw new DadoInvalidoException(
                $"O campo '{nomeDoCampo}' deve estar entre {minimo} e {maximo}, mas recebeu {valor}.");
        }

        return valor;
    }

    /// <summary>Garante que o valor pertença ao enum, barrando casts de inteiros arbitrários.</summary>
    public static TEnum ExigirEnumValido<TEnum>(TEnum valor, string nomeDoCampo) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(valor))
        {
            throw new DadoInvalidoException($"O campo '{nomeDoCampo}' recebeu um valor desconhecido: {valor}.");
        }

        return valor;
    }
}
