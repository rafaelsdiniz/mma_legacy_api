using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// O ranking de uma divisão, do campeão ao décimo quinto, com o lutador do
/// jogador encaixado onde ele chegou.
/// </summary>
/// <remarks>
/// O ranking oficial do acervo <b>nunca muda</b>. A subida do jogador é privada
/// da partida dele: dois jogadores podem ser campeões meio-pesados ao mesmo
/// tempo, cada um na sua simulação, e nenhum dos dois altera o que o outro vê
/// na página de ranking.
/// <para>
/// É por isso que aqui não existe cópia da tabela. O que a carreira guarda é um
/// número só — a posição do jogador — e a tabela exibida é derivada na leitura:
/// o ranking real com ele inserido, todo mundo daquela posição para baixo
/// descendo um degrau, e quem passar do décimo quinto caindo fora. Uma cópia
/// persistida por carreira seria dezesseis linhas a mais para sair de sincronia
/// com o acervo no primeiro rebalanceamento.
/// </para>
/// </remarks>
public sealed class TabelaDaDivisao
{
    /// <summary>Posição do campeão. Ordenar por este número já devolve a tabela pronta.</summary>
    public const int PosicaoDoCampeao = 0;

    /// <summary>Último degrau do ranking. Abaixo daqui é estar fora dele.</summary>
    public const int UltimaPosicao = 15;

    private readonly IReadOnlyDictionary<int, Lutador> _porPosicao;

    public TabelaDaDivisao(CategoriaDePeso categoria, IEnumerable<Lutador> ranqueados)
    {
        ArgumentNullException.ThrowIfNull(ranqueados);

        Categoria = categoria;
        _porPosicao = ranqueados
            .Where(atleta => atleta.PosicaoNoRanking is not null)
            .GroupBy(atleta => atleta.PosicaoNoRanking!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
    }

    public CategoriaDePeso Categoria { get; }

    public bool EstaVazia => _porPosicao.Count == 0;

    /// <summary>O atleta real naquela posição, ou nulo se a divisão não a preenche.</summary>
    public Lutador? Em(int posicao) => _porPosicao.GetValueOrDefault(posicao);

    /// <summary>
    /// O campeão da divisão — o adversário da disputa de cinturão.
    /// </summary>
    public Lutador ExigirCampeao() =>
        Em(PosicaoDoCampeao) ?? throw new RegraDeNegocioException(
            $"A divisão {Categorias.NomeDeExibicao(Categoria)} não tem campeão cadastrado.");

    /// <summary>
    /// As posições que o jogador pode desafiar a partir de onde está.
    /// </summary>
    /// <remarks>
    /// Quem está fora do ranking encara a parte de baixo: é lá que se prova que
    /// merece um número. Quem já está ranqueado desafia para <b>cima</b> — bater
    /// alguém abaixo de você não te leva a lugar nenhum, exatamente como no
    /// esporte.
    /// </remarks>
    public IReadOnlyList<int> AlvosDe(int? posicaoDoJogador, int quantidade)
    {
        var alvos = posicaoDoJogador is not { } posicao
            ? Enumerable.Range(UltimaPosicao - quantidade - 1, quantidade + 2)
            : Enumerable.Range(Math.Max(PosicaoDoCampeao + 1, posicao - quantidade - 1), quantidade + 1)
                .Where(alvo => alvo < posicao);

        return alvos
            .Where(alvo => alvo is >= PosicaoDoCampeao and <= UltimaPosicao && Em(alvo) is not null)
            .OrderBy(alvo => alvo)
            .ToList();
    }

    /// <summary>
    /// A tabela como o jogador a vê, já com ele encaixado.
    /// </summary>
    /// <param name="nomeDoJogador">Nome de cartaz, para a linha dele.</param>
    /// <param name="posicaoDoJogador">Onde ele está, ou nulo se ainda está fora.</param>
    public IReadOnlyList<LinhaDoRanking> ComOJogador(string nomeDoJogador, int? posicaoDoJogador)
    {
        var linhas = new List<LinhaDoRanking>(UltimaPosicao + 1);
        var deslocamento = 0;

        for (var posicao = PosicaoDoCampeao; posicao <= UltimaPosicao; posicao++)
        {
            if (posicao == posicaoDoJogador)
            {
                linhas.Add(LinhaDoRanking.DoJogador(posicao, nomeDoJogador));

                // A vaga é do jogador: nenhum atleta ocupa esta linha. A partir
                // daqui todos descem um degrau, e quem estava no décimo quinto
                // cai fora do ranking.
                deslocamento = 1;
                continue;
            }

            if (Em(posicao - deslocamento) is { } atleta)
            {
                linhas.Add(LinhaDoRanking.DoAcervo(posicao, atleta));
            }
        }

        return linhas;
    }
}

/// <summary>
/// As tabelas das oito divisões, para o motor consultar a do jogador.
/// </summary>
/// <remarks>
/// Vem inteiro, e não só a divisão atual, porque o jogador pode subir de peso no
/// meio da carreira. Carregar as oito de uma vez são cento e vinte e oito linhas
/// — barato o bastante para não valer a complicação de recarregar do banco no
/// meio de um passo da simulação.
/// </remarks>
public sealed class RankingDoJogo
{
    private readonly Dictionary<CategoriaDePeso, TabelaDaDivisao> _porCategoria;

    public RankingDoJogo(IEnumerable<Lutador> ranqueados)
    {
        ArgumentNullException.ThrowIfNull(ranqueados);

        _porCategoria = ranqueados
            .Where(atleta => atleta.EstaRanqueado)
            .GroupBy(atleta => atleta.Categoria!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => new TabelaDaDivisao(grupo.Key, grupo));
    }

    /// <summary>
    /// Um ranking sem ninguém. Faz o motor cair no elenco de adversários
    /// fictícios — é o que os testes de balanceamento usam para medir milhares
    /// de carreiras sem tocar em banco.
    /// </summary>
    public static RankingDoJogo Vazio { get; } = new([]);

    public TabelaDaDivisao Da(CategoriaDePeso categoria) =>
        _porCategoria.GetValueOrDefault(categoria) ?? new TabelaDaDivisao(categoria, []);
}

/// <summary>Uma linha da tabela do ranking.</summary>
/// <param name="Posicao">0 é o campeão; 1 a 15, os ranqueados.</param>
/// <param name="Slug">Nulo na linha do jogador, que não tem foto no acervo.</param>
public sealed record LinhaDoRanking(
    int Posicao,
    string Nome,
    string? Slug,
    decimal Overall,
    bool EhOJogador)
{
    public static LinhaDoRanking DoAcervo(int posicao, Lutador atleta) => new(
        posicao,
        atleta.Nome,
        atleta.Slug,
        Rules.CalculadoraDeOverall.Calcular(atleta.Atributos),
        EhOJogador: false);

    public static LinhaDoRanking DoJogador(int posicao, string nome) => new(
        posicao,
        nome,
        Slug: null,
        Overall: 0,
        EhOJogador: true);
}
