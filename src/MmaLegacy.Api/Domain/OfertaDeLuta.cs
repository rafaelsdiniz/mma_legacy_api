using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Uma luta que a organização põe na mesa. O jogador aceita uma, ou recusa
/// todas e fica parado.
/// </summary>
/// <remarks>
/// A oferta guarda os atributos do adversário, e não o overall e o estilo dele.
/// Esses dois são derivados na leitura pelas mesmas regras que classificam o
/// lutador do jogador — assim não existe a possibilidade de uma oferta gravada
/// hoje divergir da classificação de amanhã depois de um rebalanceamento.
/// <para>
/// Ofertas são efêmeras: a rodada seguinte apaga as anteriores. Só a luta
/// aceita sobrevive, e sobrevive como <see cref="LutaDaCarreira"/>.
/// </para>
/// </remarks>
public sealed class OfertaDeLuta
{
    /// <summary>
    /// Identidade própria da oferta.
    /// </summary>
    /// <remarks>
    /// Existe por causa da persistência, não do jogo. Cada rodada substitui a
    /// anterior, e se a chave fosse a posição na mesa toda troca apagaria e
    /// recriaria a mesma linha — o que o Entity Framework recusa. Com um
    /// identificador novo a cada oferta, apagar a rodada velha e inserir a nova
    /// são duas operações que não se atropelam.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>Posição da oferta na rodada atual, começando em 1. É o que o jogador escolhe.</summary>
    public int Indice { get; private set; }

    public string Adversario { get; private set; } = string.Empty;

    /// <summary>
    /// Slug do adversário quando ele é um atleta real do acervo, para o
    /// front-end achar a foto. Nulo nos adversários inventados do circuito
    /// regional e da LFA.
    /// </summary>
    public string? SlugDoAdversario { get; private set; }

    /// <summary>
    /// Posição do adversário no ranking da divisão, ou nulo se ele é fictício.
    /// </summary>
    /// <remarks>
    /// É o que a vitória converte em degrau: bater o décimo segundo colocado dá
    /// ao jogador a vaga de número doze. Sem guardar isto na oferta, o motor
    /// teria de redescobrir contra quem a luta foi depois de ela acontecer.
    /// </remarks>
    public int? PosicaoDoAdversario { get; private set; }

    /// <summary>Cartel fictício do adversário, só para dar contexto à decisão.</summary>
    public string CartelDoAdversario { get; private set; } = string.Empty;

    public Atributos AtributosDoAdversario { get; private set; } = null!;

    public NivelDaOrganizacao Organizacao { get; private set; }

    public CategoriaDePeso Categoria { get; private set; }

    public bool DisputaDeCinturao { get; private set; }

    public bool DefesaDeCinturao { get; private set; }

    public int RoundsProgramados { get; private set; }

    /// <summary>A manchete da luta, do jeito que a organização a venderia.</summary>
    public string Chamada { get; private set; } = string.Empty;

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private OfertaDeLuta()
    {
    }

    internal OfertaDeLuta(
        int indice,
        string adversario,
        string cartelDoAdversario,
        Atributos atributosDoAdversario,
        NivelDaOrganizacao organizacao,
        CategoriaDePeso categoria,
        bool disputaDeCinturao,
        bool defesaDeCinturao,
        int roundsProgramados,
        string chamada,
        string? slugDoAdversario = null,
        int? posicaoDoAdversario = null)
    {
        Id = Guid.CreateVersion7();
        Indice = indice;
        SlugDoAdversario = slugDoAdversario;
        PosicaoDoAdversario = posicaoDoAdversario;
        Adversario = adversario;
        CartelDoAdversario = cartelDoAdversario;
        AtributosDoAdversario = atributosDoAdversario;
        Organizacao = organizacao;
        Categoria = categoria;
        DisputaDeCinturao = disputaDeCinturao;
        DefesaDeCinturao = defesaDeCinturao;
        RoundsProgramados = roundsProgramados;
        Chamada = chamada;
    }

    public decimal OverallDoAdversario => CalculadoraDeOverall.Calcular(AtributosDoAdversario);

    public EstiloDeLuta EstiloDoAdversario => IdentificadorDeEstilo.Identificar(AtributosDoAdversario);

    public bool ValendoCinturao => DisputaDeCinturao || DefesaDeCinturao;

    /// <summary>
    /// Quão dura esta luta é para um lutador com o overall informado.
    /// </summary>
    /// <remarks>
    /// Derivado, como o overall e o estilo do adversário, e pelo mesmo motivo:
    /// o grau é uma relação entre dois lutadores, e o jogador de daqui a três
    /// anos não é o mesmo que recebeu a oferta.
    /// </remarks>
    public GrauDeDificuldade DificuldadeContra(decimal overallDoJogador) =>
        CalculadoraDeDificuldade.Calcular(OverallDoAdversario, overallDoJogador, ValendoCinturao);

    /// <summary>
    /// A chance de sair machucado desta luta, de 0 a 1.
    /// </summary>
    /// <remarks>
    /// Mora aqui, e não solto no motor, porque é lido em dois lugares — a tela
    /// que mostra o risco antes da decisão e o sorteio que o cobra depois. Um
    /// método só garante que os dois falem do mesmo número.
    /// </remarks>
    public double RiscoDeLesaoPara(EstadoDaCarreira estado)
    {
        ArgumentNullException.ThrowIfNull(estado);

        return CalculadoraDeLesao.Risco(
            DificuldadeContra(estado.OverallAtual),
            estado.Idade,
            estado.Atributos[Habilidade.Resistencia]);
    }
}
