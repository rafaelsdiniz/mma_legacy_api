namespace MmaLegacy.Api.Domain.Exceptions;

/// <summary>
/// Raiz de toda exceção lançada deliberadamente pelo domínio.
/// </summary>
/// <remarks>
/// Existir uma raiz comum é o que permite ao
/// <c>ManipuladorGlobalDeExcecoes</c> distinguir, em um único ponto, uma falha
/// esperada (que vira 4xx com mensagem para o jogador) de um defeito do
/// sistema (que vira 500 e mensagem genérica). Nada fora de
/// <c>Dominio</c> deve lançar estas exceções, e nada dentro dele deve lançar
/// exceções de infraestrutura.
/// </remarks>
public abstract class DominioException : Exception
{
    protected DominioException(string mensagem) : base(mensagem)
    {
    }
}
