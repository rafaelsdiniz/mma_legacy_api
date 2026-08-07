using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Alguém que o lutador já enfrentou fora do ranking e pode voltar a enfrentar.
/// </summary>
/// <remarks>
/// No UFC o adversário já tem identidade: ele é o número doze da divisão, e
/// vencê-lo significa tomar o lugar dele. No circuito regional e na LFA o
/// adversário era um nome sorteado que sumia depois da luta — e uma derrota
/// para alguém que nunca mais aparece não deixa nada além de um traço no
/// cartel. O rival existe para que deixe.
/// <para>
/// <b>A revanche não evolui o adversário de propósito.</b> Ele volta com os
/// mesmos atributos, o que faz do reencontro o único espelho honesto da
/// carreira: mesmo adversário, jogador diferente. Perder de novo para o cara
/// que te venceu há dois anos diz uma coisa muito específica sobre o que esses
/// dois anos renderam.
/// </para>
/// </remarks>
public sealed class Rival
{
    public Guid Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Cartel de fachada dele, o mesmo que apareceu na oferta original.</summary>
    public string Cartel { get; private set; } = string.Empty;

    /// <summary>Os atributos com que ele lutou, guardados para o reencontro.</summary>
    public Atributos Atributos { get; private set; } = null!;

    public int VitoriasSobreOJogador { get; private set; }

    public int DerrotasParaOJogador { get; private set; }

    public int EmpatesComOJogador { get; private set; }

    /// <summary>Número da luta do cartel em que se enfrentaram pela última vez.</summary>
    public int OrdemDoUltimoEncontro { get; private set; }

    /// <summary>Como terminou o último encontro, do ponto de vista do jogador.</summary>
    public ResultadoDaLuta ResultadoDoUltimoEncontro { get; private set; }

    public MetodoDeEncerramento MetodoDoUltimoEncontro { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Rival()
    {
    }

    internal Rival(string nome, string cartel, Atributos atributos)
    {
        Id = Guid.CreateVersion7();
        Nome = nome;
        Cartel = cartel;
        Atributos = atributos;
    }

    /// <summary>Ele está na frente no confronto direto. É quem vale a pena reencontrar.</summary>
    public bool TemContaAAcertar => VitoriasSobreOJogador > DerrotasParaOJogador;

    public int TotalDeEncontros => VitoriasSobreOJogador + DerrotasParaOJogador + EmpatesComOJogador;

    /// <summary>Anota mais um encontro, do ponto de vista do jogador.</summary>
    internal void Anotar(ResultadoDaLuta resultado, MetodoDeEncerramento metodo, int ordemDaLuta)
    {
        switch (resultado)
        {
            case ResultadoDaLuta.Vitoria:
                DerrotasParaOJogador++;
                break;
            case ResultadoDaLuta.Derrota:
                VitoriasSobreOJogador++;
                break;
            default:
                EmpatesComOJogador++;
                break;
        }

        ResultadoDoUltimoEncontro = resultado;
        MetodoDoUltimoEncontro = metodo;
        OrdemDoUltimoEncontro = ordemDaLuta;
    }
}
