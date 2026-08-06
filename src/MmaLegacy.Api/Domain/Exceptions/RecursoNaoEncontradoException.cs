namespace MmaLegacy.Api.Domain.Exceptions;

/// <summary>
/// O recurso pedido não existe. Vira <c>404 Not Found</c>.
/// </summary>
public sealed class RecursoNaoEncontradoException : DominioException
{
    private RecursoNaoEncontradoException(string mensagem) : base(mensagem)
    {
    }

    /// <summary>
    /// Monta a mensagem no padrão "&lt;recurso&gt; '&lt;identificador&gt;' não foi encontrado."
    /// para que toda resposta 404 da API tenha o mesmo formato.
    /// </summary>
    public static RecursoNaoEncontradoException Para(string recurso, object identificador) =>
        new($"{recurso} '{identificador}' não foi encontrado.");
}
