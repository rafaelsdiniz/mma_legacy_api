using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Uma das oito rodadas do draft: o atleta que foi apresentado ao jogador e,
/// depois da decisão, qual habilidade ele cedeu e com que nota.
/// </summary>
/// <remarks>
/// A nota fica gravada aqui, e não é lida do acervo na hora de montar o
/// lutador. Assim um rebalanceamento futuro das notas editoriais não reescreve
/// o passado: partidas antigas continuam mostrando o lutador que o jogador
/// realmente montou.
/// </remarks>
public sealed class RodadaDeDraft
{
    /// <summary>Posição da rodada no draft, de 1 a 8.</summary>
    public int Ordem { get; private set; }

    /// <summary>O atleta do acervo apresentado nesta rodada.</summary>
    public Guid LutadorId { get; private set; }

    /// <summary>Nome do atleta no momento do sorteio, para o histórico da partida.</summary>
    public string LutadorNome { get; private set; } = string.Empty;

    /// <summary>Habilidade escolhida, ou <c>null</c> enquanto a rodada não aconteceu.</summary>
    public Habilidade? HabilidadeEscolhida { get; private set; }

    /// <summary>Nota que o atleta tinha na habilidade escolhida.</summary>
    public int? NotaObtida { get; private set; }

    public bool Concluida => HabilidadeEscolhida.HasValue;

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private RodadaDeDraft()
    {
    }

    internal RodadaDeDraft(int ordem, Lutador lutador)
    {
        Ordem = ordem;
        LutadorId = lutador.Id;
        LutadorNome = lutador.Nome;
    }

    /// <summary>
    /// Grava a decisão do jogador. É <c>internal</c> de propósito: só a
    /// <see cref="Partida"/> pode chamar, porque só ela sabe se a habilidade
    /// ainda está livre e se é mesmo a vez deste atleta.
    /// </summary>
    internal void Registrar(Habilidade habilidade, int nota)
    {
        HabilidadeEscolhida = habilidade;
        NotaObtida = nota;
    }
}
