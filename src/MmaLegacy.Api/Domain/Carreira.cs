using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// A trajetória completa do lutador, da estreia à aposentadoria.
/// </summary>
/// <remarks>
/// O motor de carreira apenas empilha lutas com <see cref="RegistrarLuta"/>.
/// Todo o resumo — cartel, métodos de vitória, cinturões, defesas, sequências,
/// tempo de reinado — é recalculado de uma vez em <see cref="Encerrar"/> a
/// partir da lista de lutas. Contador que o motor incrementa a cada passo é
/// contador que uma hora sai do lugar; contador derivado, não.
/// </remarks>
public sealed class Carreira
{
    private readonly List<LutaDaCarreira> _lutas = [];

    public Guid Id { get; private set; }

    /// <summary>Partida que deu origem a esta carreira.</summary>
    public Guid PartidaId { get; private set; }

    public int IdadeDeEstreia { get; private set; }
    public int IdadeDeAposentadoria { get; private set; }

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

    /// <summary>Maior overall atingido, já que os atributos evoluem e decaem com a idade.</summary>
    public decimal OverallMaximo { get; private set; }

    /// <summary>Categoria em que encerrou a carreira.</summary>
    public CategoriaDePeso CategoriaFinal { get; private set; }

    public NivelDeLegado Legado { get; private set; }

    /// <summary>Pontuação bruta que produziu o <see cref="Legado"/>, útil para ranking e balanceamento.</summary>
    public int PontuacaoDeLegado { get; private set; }

    public IReadOnlyList<LutaDaCarreira> Lutas => _lutas.AsReadOnly();

    /// <summary>Total de lutas disputadas.</summary>
    public int TotalDeLutas => _lutas.Count;

    /// <summary>Cartel no formato "29-2-0".</summary>
    public string Cartel => $"{Vitorias}-{Derrotas}-{Empates}";

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Carreira()
    {
    }

    public static Carreira Iniciar(Guid partidaId, int idadeDeEstreia, CategoriaDePeso categoriaInicial) => new()
    {
        Id = Guid.CreateVersion7(),
        PartidaId = partidaId,
        IdadeDeEstreia = idadeDeEstreia,
        CategoriaFinal = categoriaInicial
    };

    internal void RegistrarLuta(LutaDaCarreira luta)
    {
        ArgumentNullException.ThrowIfNull(luta);
        _lutas.Add(luta);
    }

    /// <summary>
    /// Fecha a carreira e deriva todo o resumo das lutas registradas.
    /// </summary>
    /// <param name="idadeDeAposentadoria">Idade em que pendurou as luvas.</param>
    /// <param name="overallMaximo">Maior overall observado ao longo da evolução.</param>
    internal void Encerrar(int idadeDeAposentadoria, decimal overallMaximo)
    {
        RegraDeNegocioException.Se(
            _lutas.Count == 0,
            "Não é possível encerrar uma carreira sem nenhuma luta registrada.");

        IdadeDeAposentadoria = idadeDeAposentadoria;
        OverallMaximo = overallMaximo;

        var lutasEmOrdem = _lutas.OrderBy(luta => luta.Ordem).ToList();
        ContabilizarCartel(lutasEmOrdem);
        ContabilizarReinados(lutasEmOrdem);

        CategoriaFinal = lutasEmOrdem[^1].Categoria;
        AposentouInvicto = Vitorias > 0 && Derrotas == 0 && Empates == 0;
    }

    internal void DefinirLegado(NivelDeLegado nivel, int pontuacao)
    {
        Legado = nivel;
        PontuacaoDeLegado = pontuacao;
    }

    /// <summary>Soma vitórias, derrotas, métodos e a maior sequência invicta.</summary>
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
    /// uma defesa perdida — ou na aposentadoria, com o cinturão na cintura.
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
            AnosComoCampeao += IdadeDeAposentadoria - reinadoEmAberto;
        }

        FoiDuploCampeao = categoriasComCinturao.Count >= 2;
    }
}
