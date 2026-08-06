using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Uma partida do jogo, da ficha de inscrição ao veredito de legado. É a raiz
/// de agregação: toda regra do draft passa por aqui.
/// </summary>
/// <remarks>
/// O front-end nunca envia notas. Ele diz apenas "quero a habilidade X do
/// atleta Y", e é esta classe que confere se o atleta é mesmo o da rodada
/// atual, se a habilidade continua livre e qual é a nota verdadeira. Trocar
/// valores pelo navegador não muda o lutador montado.
/// </remarks>
public sealed class Partida
{
    private readonly List<RodadaDeDraft> _rodadas = [];

    public Guid Id { get; private set; }

    /// <summary>
    /// Semente da partida. Fixa o sorteio dos atletas e, derivada, a simulação
    /// da carreira — o que torna o resultado reproduzível e permite o draft
    /// diário igual para todo mundo.
    /// </summary>
    public int Seed { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    public StatusDaPartida Status { get; private set; }

    /// <summary>
    /// Quanta informação o jogador vê durante o draft.
    /// </summary>
    /// <remarks>
    /// Fica na partida, e não na ficha, porque é regra de jogo e não identidade
    /// do lutador. Escolhida na criação e imutável depois: trocar de nível no
    /// meio do draft permitiria espiar as notas no fácil e voltar para o difícil.
    /// </remarks>
    public NivelDeDificuldade NivelDeDificuldade { get; private set; }

    public FichaDeInscricao Ficha { get; private set; } = null!;

    /// <summary>O lutador montado. Só existe depois da oitava escolha.</summary>
    public LutadorCriado? Lutador { get; private set; }

    /// <summary>A carreira simulada. Só existe depois que o jogador pede a simulação.</summary>
    public Carreira? Carreira { get; private set; }

    public IReadOnlyList<RodadaDeDraft> Rodadas => _rodadas.OrderBy(rodada => rodada.Ordem).ToList();

    /// <summary>Quantas das oito escolhas já foram feitas.</summary>
    public int EscolhasFeitas => _rodadas.Count(rodada => rodada.Concluida);

    /// <summary>
    /// Semente da simulação de carreira, derivada da semente da partida.
    /// </summary>
    /// <remarks>
    /// Usar a mesma semente nas duas etapas faria o sorteio dos atletas e o
    /// sorteio dos adversários andarem juntos, criando correlações estranhas
    /// entre o draft e a carreira. A derivação é aritmética simples de propósito:
    /// precisa dar o mesmo número em qualquer máquina e em qualquer execução.
    /// </remarks>
    public int SeedDaCarreira => unchecked((Seed * 31) + 17);

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Partida()
    {
    }

    /// <summary>
    /// Abre uma partida com o draft já sorteado.
    /// </summary>
    /// <param name="ficha">Identidade do lutador informada pelo jogador.</param>
    /// <param name="seed">Semente que fixa este sorteio.</param>
    /// <param name="atletasSorteados">Os oito atletas, na ordem em que serão apresentados.</param>
    public static Partida Iniciar(
        FichaDeInscricao ficha,
        int seed,
        IReadOnlyList<Lutador> atletasSorteados,
        NivelDeDificuldade nivelDeDificuldade = NivelDeDificuldade.Facil)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        ArgumentNullException.ThrowIfNull(atletasSorteados);
        DadoInvalidoException.ExigirEnumValido(nivelDeDificuldade, nameof(nivelDeDificuldade));

        RegraDeNegocioException.Se(
            atletasSorteados.Count != Habilidades.Quantidade,
            $"O draft precisa de exatamente {Habilidades.Quantidade} atletas, " +
            $"mas foram sorteados {atletasSorteados.Count}.");

        RegraDeNegocioException.Se(
            atletasSorteados.Select(atleta => atleta.Id).Distinct().Count() != atletasSorteados.Count,
            "Um mesmo atleta não pode aparecer duas vezes no mesmo draft.");

        var partida = new Partida
        {
            Id = Guid.CreateVersion7(),
            Seed = seed,
            CriadaEm = DateTimeOffset.UtcNow,
            Status = StatusDaPartida.DraftEmAndamento,
            Ficha = ficha,
            NivelDeDificuldade = nivelDeDificuldade
        };

        for (var ordem = 0; ordem < atletasSorteados.Count; ordem++)
        {
            partida._rodadas.Add(new RodadaDeDraft(ordem + 1, atletasSorteados[ordem]));
        }

        return partida;
    }

    /// <summary>A rodada que está esperando uma decisão do jogador.</summary>
    public RodadaDeDraft RodadaAtual()
    {
        var rodada = _rodadas.OrderBy(item => item.Ordem).FirstOrDefault(item => !item.Concluida);

        return rodada ?? throw new RegraDeNegocioException(
            "O draft desta partida já foi concluído e não há rodada em aberto.");
    }

    /// <summary>
    /// Habilidades que ainda podem ser escolhidas. É o que o front-end usa para
    /// desabilitar os botões já ocupados.
    /// </summary>
    public IReadOnlyList<Habilidade> HabilidadesDisponiveis()
    {
        var ocupadas = _rodadas
            .Where(rodada => rodada.HabilidadeEscolhida.HasValue)
            .Select(rodada => rodada.HabilidadeEscolhida!.Value)
            .ToHashSet();

        return Habilidades.Todas.Where(habilidade => !ocupadas.Contains(habilidade)).ToList();
    }

    /// <summary>
    /// Registra a escolha do jogador para a rodada atual e, se foi a oitava,
    /// fecha o draft e monta o lutador.
    /// </summary>
    /// <param name="atleta">O atleta apresentado na rodada, carregado do acervo.</param>
    /// <param name="habilidade">A habilidade que o jogador quer levar dele.</param>
    public void EscolherHabilidade(Lutador atleta, Habilidade habilidade)
    {
        ArgumentNullException.ThrowIfNull(atleta);

        RegraDeNegocioException.Se(
            Status != StatusDaPartida.DraftEmAndamento,
            "O draft desta partida já foi concluído.");

        DadoInvalidoException.ExigirEnumValido(habilidade, nameof(habilidade));

        var rodada = RodadaAtual();

        RegraDeNegocioException.Se(
            atleta.Id != rodada.LutadorId,
            $"{atleta.Nome} não é o atleta da rodada {rodada.Ordem}. " +
            $"A vez é de {rodada.LutadorNome}.");

        RegraDeNegocioException.Se(
            !HabilidadesDisponiveis().Contains(habilidade),
            $"{Habilidades.NomeDeExibicao(habilidade)} já foi preenchida em uma rodada anterior.");

        rodada.Registrar(habilidade, atleta.Atributos[habilidade]);

        if (_rodadas.All(item => item.Concluida))
        {
            ConcluirDraft();
        }
    }

    /// <summary>
    /// Guarda a carreira simulada e encerra a partida.
    /// </summary>
    public void RegistrarCarreira(Carreira carreira)
    {
        ArgumentNullException.ThrowIfNull(carreira);

        RegraDeNegocioException.Se(
            Status != StatusDaPartida.DraftConcluido,
            Status == StatusDaPartida.DraftEmAndamento
                ? "Termine o draft antes de simular a carreira."
                : "A carreira desta partida já foi simulada.");

        Carreira = carreira;
        Status = StatusDaPartida.CarreiraSimulada;
    }

    /// <summary>
    /// Devolve o lutador montado, garantindo que o draft terminou. Evita que
    /// cada chamador precise checar o status e tratar um nulo.
    /// </summary>
    public LutadorCriado ExigirLutadorMontado() =>
        Lutador ?? throw new RegraDeNegocioException(
            $"O lutador ainda não foi montado: faltam " +
            $"{Habilidades.Quantidade - EscolhasFeitas} escolha(s) no draft.");

    /// <summary>Devolve a carreira simulada, garantindo que a simulação já rodou.</summary>
    public Carreira ExigirCarreiraSimulada() =>
        Carreira ?? throw new RegraDeNegocioException(
            "A carreira desta partida ainda não foi simulada.");

    private void ConcluirDraft()
    {
        var notas = _rodadas.ToDictionary(
            rodada => rodada.HabilidadeEscolhida!.Value,
            rodada => rodada.NotaObtida!.Value);

        Lutador = new LutadorCriado(Atributos.APartirDe(notas));
        Status = StatusDaPartida.DraftConcluido;
    }
}
