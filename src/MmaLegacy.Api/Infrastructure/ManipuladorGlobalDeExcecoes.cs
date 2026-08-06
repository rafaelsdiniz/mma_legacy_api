using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Infrastructure;

/// <summary>
/// Converte exceção em resposta HTTP, em um lugar só.
/// </summary>
/// <remarks>
/// Nenhum controller tem <c>try/catch</c> porque tudo passa por aqui. A
/// tradução é dirigida pela hierarquia de <see cref="DominioException"/>: falha
/// esperada vira 4xx com a mensagem escrita para o jogador; qualquer outra
/// exceção vira 500 com texto genérico e o detalhe fica no log.
/// <para>
/// Essa última parte é deliberada. Devolver <c>ex.Message</c> em erro não
/// tratado vaza nome de tabela, caminho de arquivo e string de conexão para
/// quem chamou a API.
/// </para>
/// </remarks>
public sealed class ManipuladorGlobalDeExcecoes(
    IProblemDetailsService servicoDeProblemas,
    ILogger<ManipuladorGlobalDeExcecoes> log) : IExceptionHandler
{
    private const string MensagemGenerica =
        "Não foi possível concluir a operação. Tente novamente em instantes.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excecao,
        CancellationToken cancelamento)
    {
        var (status, titulo, detalhe) = Traduzir(excecao);

        if (status == StatusCodes.Status500InternalServerError)
        {
            log.LogError(excecao, "Falha não tratada em {Metodo} {Caminho}",
                contexto.Request.Method, contexto.Request.Path);
        }
        else
        {
            log.LogInformation("Requisição recusada em {Metodo} {Caminho}: {Detalhe}",
                contexto.Request.Method, contexto.Request.Path, detalhe);
        }

        contexto.Response.StatusCode = status;

        return await servicoDeProblemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = excecao,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = detalhe,
                Instance = $"{contexto.Request.Method} {contexto.Request.Path}"
            }
        });
    }

    private static (int Status, string Titulo, string Detalhe) Traduzir(Exception excecao) => excecao switch
    {
        RecursoNaoEncontradoException naoEncontrado =>
            (StatusCodes.Status404NotFound, "Recurso não encontrado", naoEncontrado.Message),

        // A requisição faz sentido, mas conflita com o estado atual da partida:
        // habilidade já ocupada, draft encerrado, carreira já simulada.
        RegraDeNegocioException regraViolada =>
            (StatusCodes.Status409Conflict, "Jogada inválida", regraViolada.Message),

        DadoInvalidoException dadoInvalido =>
            (StatusCodes.Status400BadRequest, "Dados inválidos", dadoInvalido.Message),

        OperationCanceledException =>
            (StatusCodes.Status499ClientClosedRequest, "Requisição cancelada",
                "A requisição foi cancelada antes de terminar."),

        _ => (StatusCodes.Status500InternalServerError, "Erro interno", MensagemGenerica)
    };
}
