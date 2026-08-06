namespace MmaLegacy.Api.Domain.Exceptions;

/// <summary>
/// A operação é válida em si, mas conflita com o estado atual da partida —
/// escolher uma habilidade já ocupada, simular a carreira antes de terminar o
/// draft, usar um atleta que não está na rodada. Vira <c>409 Conflict</c>.
/// </summary>
/// <remarks>
/// A mensagem é escrita para ser exibida ao jogador, então deve descrever o que
/// aconteceu em linguagem do jogo, nunca em termos técnicos.
/// </remarks>
public sealed class RegraDeNegocioException : DominioException
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }

    /// <summary>Lança a exceção quando <paramref name="condicao"/> for verdadeira.</summary>
    public static void Se(bool condicao, string mensagem)
    {
        if (condicao)
        {
            throw new RegraDeNegocioException(mensagem);
        }
    }
}
