using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Uma lesão em tratamento. Enquanto ela existe, o lutador não recebe ofertas —
/// só tem o que fazer de fisioterapia.
/// </summary>
/// <remarks>
/// A lesão é a única coisa do jogo que consome calendário sem que o jogador
/// decida nada. É de propósito: aceitar a luta brutal foi a decisão, e o preço
/// dela é justamente perder o direito de decidir por um tempo.
/// <para>
/// A sequela de atributo não mora aqui. Ela é aplicada de uma vez, no momento
/// em que a lesão acontece, direto sobre os atributos do estado — guardar uma
/// penalidade viva que alguém precisa lembrar de tirar depois é a forma mais
/// confiável de um dia esquecer de tirar.
/// </para>
/// </remarks>
public sealed class Lesao
{
    public TipoDeLesao Tipo { get; private set; }

    public GravidadeDaLesao Gravidade { get; private set; }

    /// <summary>Compromissos de calendário que a lesão custou no total.</summary>
    public int Afastamento { get; private set; }

    /// <summary>Quantos compromissos ainda faltam para voltar a lutar.</summary>
    public int CompromissosRestantes { get; private set; }

    /// <summary>Idade em que se machucou, para a tela poder contar a história.</summary>
    public int IdadeQuandoOcorreu { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Lesao()
    {
    }

    internal Lesao(TipoDeLesao tipo, GravidadeDaLesao gravidade, int idadeQuandoOcorreu)
    {
        Tipo = tipo;
        Gravidade = gravidade;
        Afastamento = AfastamentoDe(gravidade);
        CompromissosRestantes = Afastamento;
        IdadeQuandoOcorreu = idadeQuandoOcorreu;
    }

    public bool Sarou => CompromissosRestantes <= 0;

    /// <summary>Quantos compromissos de calendário cada gravidade custa.</summary>
    public static int AfastamentoDe(GravidadeDaLesao gravidade) => (int)gravidade;

    /// <summary>Quantos pontos de atributo a lesão leva para sempre.</summary>
    public static int SequelaDe(GravidadeDaLesao gravidade) => gravidade switch
    {
        GravidadeDaLesao.Leve => 0,
        GravidadeDaLesao.Moderada => 1,
        _ => 2
    };

    /// <summary>A habilidade que esta lesão cobra ao acontecer.</summary>
    public static Habilidade HabilidadeAfetadaPor(TipoDeLesao tipo) => tipo switch
    {
        TipoDeLesao.MaoFraturada => Habilidade.Potencia,
        TipoDeLesao.JoelhoLesionado => Habilidade.Velocidade,
        TipoDeLesao.CostelaTrincada => Habilidade.Cardio,
        TipoDeLesao.Concussao => Habilidade.Resistencia,

        // Corte não deixa sequela: sara e some. Aponta para resistência apenas
        // porque a assinatura precisa de uma habilidade, e a sequela dele é zero.
        _ => Habilidade.Resistencia
    };

    /// <summary>Passa um compromisso de recuperação.</summary>
    internal void Tratar() => CompromissosRestantes = Math.Max(0, CompromissosRestantes - 1);
}
