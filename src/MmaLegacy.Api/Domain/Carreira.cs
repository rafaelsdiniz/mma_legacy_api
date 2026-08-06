using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// A trajetória do lutador, da estreia à aposentadoria. Enquanto está em
/// andamento, é também a mesa em que as ofertas de luta ficam esperando uma
/// decisão do jogador.
/// </summary>
/// <remarks>
/// Todo o resumo — cartel, métodos de vitória, cinturões, defesas, sequências,
/// tempo de reinado — é <b>derivado</b> da lista de lutas, nunca somado à mão
/// por quem registra uma luta nova. Contador que se incrementa a cada passo é
/// contador que uma hora sai do lugar; contador derivado, não.
/// <para>
/// A derivação roda a cada luta registrada, e não só no fim, porque agora o
/// jogador vê o próprio cartel crescer entre uma decisão e a seguinte.
/// <see cref="Encerrar"/> apenas fecha a conta e acrescenta o que só existe no
/// fim: idade de aposentadoria, categoria final e o motivo de tudo ter acabado.
/// </para>
/// </remarks>
public sealed class Carreira
{
    private readonly List<LutaDaCarreira> _lutas = [];
    private readonly List<OfertaDeLuta> _ofertas = [];

    public Guid Id { get; private set; }

    /// <summary>Partida que deu origem a esta carreira.</summary>
    public Guid PartidaId { get; private set; }

    /// <summary>Onde o lutador está agora. Continua sendo atualizado até a aposentadoria.</summary>
    public EstadoDaCarreira Estado { get; private set; } = null!;

    public int IdadeDeEstreia { get; private set; }
    public int IdadeDeAposentadoria { get; private set; }

    /// <summary>Falso enquanto ainda há luta pela frente.</summary>
    public bool Encerrada { get; private set; }

    /// <summary>Por que a carreira acabou. Nulo enquanto ela não acabou.</summary>
    public MotivoDoEncerramento? MotivoDoEncerramento { get; private set; }

    public int Vitorias { get; private set; }
    public int Derrotas { get; private set; }
    public int Empates { get; private set; }

    public int VitoriasPorNocaute { get; private set; }
    public int VitoriasPorFinalizacao { get; private set; }
    public int VitoriasPorDecisao { get; private set; }

    /// <summary>Quantas vezes conquistou um cinturão, somando todas as categorias.</summary>
    public int CinturoesConquistados { get; private set; }

    public int DefesasDeCinturao { get; private set; }

    /// <summary>Anos somados em que esteve com o cinturão na cintura.</summary>
    public int AnosComoCampeao { get; private set; }

    public bool FoiCampeao => CinturoesConquistados > 0;

    /// <summary>Conquistou cinturões em duas categorias diferentes.</summary>
    public bool FoiDuploCampeao { get; private set; }

    public bool AposentouInvicto { get; private set; }

    public int MaiorSequenciaDeVitorias { get; private set; }

    /// <summary>Sequência de vitórias em aberto — a que o jogador está vivendo agora.</summary>
    public int SequenciaAtualDeVitorias { get; private set; }

    /// <summary>Maior overall atingido, já que os atributos evoluem e decaem com a idade.</summary>
    public decimal OverallMaximo { get; private set; }

    /// <summary>Categoria em que encerrou a carreira.</summary>
    public CategoriaDePeso CategoriaFinal { get; private set; }

    public NivelDeLegado Legado { get; private set; }

    /// <summary>Pontuação bruta que produziu o <see cref="Legado"/>, útil para ranking e balanceamento.</summary>
    public int PontuacaoDeLegado { get; private set; }

    public IReadOnlyList<LutaDaCarreira> Lutas => _lutas.OrderBy(luta => luta.Ordem).ToList();

    /// <summary>As lutas na mesa agora. Vazia quando a carreira acabou.</summary>
    public IReadOnlyList<OfertaDeLuta> Ofertas => _ofertas.OrderBy(oferta => oferta.Indice).ToList();

    /// <summary>Total de lutas disputadas.</summary>
    public int TotalDeLutas => _lutas.Count;

    /// <summary>Cartel no formato "29-2-0".</summary>
    public string Cartel => $"{Vitorias}-{Derrotas}-{Empates}";

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Carreira()
    {
    }

    public static Carreira Iniciar(Guid partidaId, FichaDeInscricao ficha, LutadorCriado lutador)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        ArgumentNullException.ThrowIfNull(lutador);

        return new Carreira
        {
            Id = Guid.CreateVersion7(),
            PartidaId = partidaId,
            IdadeDeEstreia = ficha.IdadeInicial,
            CategoriaFinal = ficha.CategoriaDePeso,
            OverallMaximo = lutador.Overall,
            Estado = EstadoDaCarreira.Inicial(ficha, lutador)
        };
    }

    /// <summary>Põe uma nova rodada de ofertas na mesa, descartando a anterior.</summary>
    internal void ReceberOfertas(IEnumerable<OfertaDeLuta> ofertas)
    {
        ArgumentNullException.ThrowIfNull(ofertas);
        ExigirEmAndamento();

        _ofertas.Clear();
        _ofertas.AddRange(ofertas);
    }

    /// <summary>
    /// Encontra a oferta que o jogador aceitou.
    /// </summary>
    /// <remarks>
    /// O front-end manda apenas o índice, e é aqui que se confere se ele existe
    /// mesmo na rodada atual. Uma tela velha reenviando a escolha de uma rodada
    /// já resolvida recebe erro, e não uma luta contra um adversário que a
    /// organização nem ofereceu.
    /// </remarks>
    public OfertaDeLuta ExigirOferta(int indice)
    {
        ExigirEmAndamento();

        return _ofertas.FirstOrDefault(oferta => oferta.Indice == indice)
            ?? throw new RegraDeNegocioException(
                _ofertas.Count == 0
                    ? "Não há nenhuma oferta de luta na mesa agora."
                    : $"A oferta {indice} não está entre as {_ofertas.Count} desta rodada.");
    }

    internal void LimparOfertas() => _ofertas.Clear();

    /// <summary>Registra a luta disputada e recalcula todo o resumo a partir do cartel.</summary>
    internal void RegistrarLuta(LutaDaCarreira luta)
    {
        ArgumentNullException.ThrowIfNull(luta);
        ExigirEmAndamento();

        _lutas.Add(luta);
        RecalcularResumo();
    }

    /// <summary>
    /// Fecha a carreira.
    /// </summary>
    /// <param name="motivo">O que pôs fim a tudo.</param>
    internal void Encerrar(MotivoDoEncerramento motivo)
    {
        ExigirEmAndamento();

        IdadeDeAposentadoria = Estado.Idade;
        OverallMaximo = Estado.OverallMaximo;
        Encerrada = true;
        MotivoDoEncerramento = motivo;

        _ofertas.Clear();

        RecalcularResumo();

        CategoriaFinal = _lutas.Count > 0
            ? Lutas[^1].Categoria
            : Estado.Categoria;

        AposentouInvicto = Vitorias > 0 && Derrotas == 0 && Empates == 0;
    }

    internal void DefinirLegado(NivelDeLegado nivel, int pontuacao)
    {
        Legado = nivel;
        PontuacaoDeLegado = pontuacao;
    }

    private void ExigirEmAndamento() => RegraDeNegocioException.Se(
        Encerrada,
        "Esta carreira já foi encerrada.");

    private void RecalcularResumo()
    {
        var lutasEmOrdem = Lutas;
        ContabilizarCartel(lutasEmOrdem);
        ContabilizarReinados(lutasEmOrdem);
    }

    /// <summary>Soma vitórias, derrotas, métodos e as sequências invictas.</summary>
    private void ContabilizarCartel(IReadOnlyList<LutaDaCarreira> lutasEmOrdem)
    {
        Vitorias = Derrotas = Empates = 0;
        VitoriasPorNocaute = VitoriasPorFinalizacao = VitoriasPorDecisao = 0;
        MaiorSequenciaDeVitorias = 0;

        var sequenciaAtual = 0;

        foreach (var luta in lutasEmOrdem)
        {
            switch (luta.Resultado)
            {
                case ResultadoDaLuta.Vitoria:
                    Vitorias++;
                    sequenciaAtual++;
                    MaiorSequenciaDeVitorias = Math.Max(MaiorSequenciaDeVitorias, sequenciaAtual);
                    ContabilizarMetodo(luta.Metodo);
                    break;

                case ResultadoDaLuta.Derrota:
                    Derrotas++;
                    sequenciaAtual = 0;
                    break;

                case ResultadoDaLuta.Empate:
                    Empates++;
                    sequenciaAtual = 0;
                    break;
            }
        }

        SequenciaAtualDeVitorias = sequenciaAtual;
    }

    private void ContabilizarMetodo(MetodoDeEncerramento metodo)
    {
        switch (metodo)
        {
            case MetodoDeEncerramento.Nocaute:
                VitoriasPorNocaute++;
                break;
            case MetodoDeEncerramento.Finalizacao:
                VitoriasPorFinalizacao++;
                break;
            case MetodoDeEncerramento.Decisao:
                VitoriasPorDecisao++;
                break;
        }
    }

    /// <summary>
    /// Percorre as lutas de título como uma máquina de estados para descobrir
    /// quantos cinturões vieram, quantas defesas foram feitas e por quantos anos
    /// o lutador reinou. Um reinado começa numa disputa vencida e só termina em
    /// uma defesa perdida — ou na idade de hoje, com o cinturão ainda na cintura.
    /// </summary>
    private void ContabilizarReinados(IReadOnlyList<LutaDaCarreira> lutasEmOrdem)
    {
        CinturoesConquistados = 0;
        DefesasDeCinturao = 0;
        AnosComoCampeao = 0;

        var categoriasComCinturao = new HashSet<CategoriaDePeso>();
        int? idadeEmQueConquistou = null;

        foreach (var luta in lutasEmOrdem)
        {
            var venceu = luta.Resultado == ResultadoDaLuta.Vitoria;

            if (luta.DisputaDeCinturao && venceu)
            {
                CinturoesConquistados++;
                categoriasComCinturao.Add(luta.Categoria);
                idadeEmQueConquistou ??= luta.Idade;
            }
            else if (luta.DefesaDeCinturao)
            {
                if (venceu)
                {
                    DefesasDeCinturao++;
                }
                else if (idadeEmQueConquistou is { } inicioDoReinado)
                {
                    AnosComoCampeao += luta.Idade - inicioDoReinado;
                    idadeEmQueConquistou = null;
                }
            }
        }

        if (idadeEmQueConquistou is { } reinadoEmAberto)
        {
            AnosComoCampeao += Estado.Idade - reinadoEmAberto;
        }

        FoiDuploCampeao = categoriasComCinturao.Count >= 2;
    }
}
