using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Onde o lutador está agora: idade, atributos do momento, degrau na escada e
/// as sequências que decidem o próximo passo.
/// </summary>
/// <remarks>
/// Antes isto era uma classe privada dentro do motor de carreira, viva apenas
/// durante o laço que simulava a vida inteira de uma vez. Com a carreira jogada
/// luta a luta, cada decisão do jogador é uma requisição HTTP diferente — e o
/// que existia só na pilha precisou virar estado persistido.
/// <para>
/// O que <b>não</b> mora aqui é tão importante quanto o que mora: cartel,
/// vitórias e derrotas continuam sendo derivados da lista de lutas em
/// <see cref="Carreira"/>. Duplicá-los aqui criaria duas fontes da verdade que
/// mais cedo ou mais tarde discordariam.
/// </para>
/// </remarks>
public sealed class EstadoDaCarreira
{
    public int Idade { get; private set; }

    /// <summary>Atributos do lutador hoje, já passados por todas as curvas de evolução.</summary>
    public Atributos Atributos { get; private set; } = null!;

    public CategoriaDePeso Categoria { get; private set; }

    public EtapaDaCarreira Etapa { get; private set; }

    /// <summary>Maior overall já atingido, que é o pico do atleta e não o de agora.</summary>
    public decimal OverallMaximo { get; private set; }

    /// <summary>Vitórias acumuladas no degrau atual, zeradas por qualquer tropeço.</summary>
    public int VitoriasNaEtapa { get; private set; }

    public int DerrotasSeguidas { get; private set; }

    /// <summary>Rodadas de oferta seguidas em que o jogador não aceitou nada.</summary>
    public int RecusasSeguidas { get; private set; }

    public int NocautesSofridos { get; private set; }

    /// <summary>Nocautes sofridos no ano corrente, cobrados na virada da temporada.</summary>
    public int NocautesSofridosNoAno { get; private set; }

    public int DefesasNaCategoria { get; private set; }

    public bool EhCampeao { get; private set; }

    public bool JaMudouDeCategoria { get; private set; }

    /// <summary>Quanto os adversários deste degrau vêm acima da faixa normal.</summary>
    public int AjusteDeOverallDoAdversario { get; private set; }

    /// <summary>
    /// Espaços da temporada já consumidos. Lutar gasta um; recusar todas as
    /// ofertas também — é assim que o tempo cobra a indecisão.
    /// </summary>
    public int CompromissosNaTemporada { get; private set; }

    /// <summary>Quantas vezes já foi dispensado por uma organização.</summary>
    public int VezesDispensado { get; private set; }

    /// <summary>
    /// A lesão que o lutador está tratando agora, ou nulo se ele está inteiro.
    /// </summary>
    /// <remarks>
    /// Some assim que sara. Guardar lesão curada aqui criaria dois estados
    /// parecidos — "machucado" e "machucado mas já pode lutar" — e todo lugar
    /// que lê isto teria de saber a diferença. O histórico de quantas vieram
    /// fica em <see cref="LesoesSofridas"/>, que é o que a aposentadoria olha.
    /// </remarks>
    public Lesao? LesaoAtual { get; private set; }

    /// <summary>Quantas lesões o corpo já levou nesta carreira.</summary>
    public int LesoesSofridas { get; private set; }

    /// <summary>Está parado se recuperando, e por isso não recebe ofertas.</summary>
    public bool EstaLesionado => LesaoAtual is not null;

    /// <summary>
    /// Posição do jogador no ranking da divisão: <c>0</c> é o campeão, <c>1</c>
    /// a <c>15</c> os ranqueados, e <c>null</c> quem ainda não entrou.
    /// </summary>
    /// <remarks>
    /// É o único número guardado sobre o ranking. A tabela que o jogador vê é
    /// derivada dele na leitura, pela <see cref="TabelaDaDivisao"/> — o ranking
    /// oficial do acervo nunca é alterado, porque a subida é privada da partida.
    /// </remarks>
    public int? PosicaoNoRanking { get; private set; }

    /// <summary>Já tem número ao lado do nome na divisão.</summary>
    public bool EstaRanqueado => PosicaoNoRanking is not null;

    /// <summary>
    /// Quantas decisões já foram tomadas nesta carreira.
    /// </summary>
    /// <remarks>
    /// É o contador que substitui o <c>Sorteio</c> único que o motor mantinha na
    /// pilha. Com a carreira jogada em várias requisições não existe mais uma
    /// sequência aleatória viva do começo ao fim, então cada passo deriva a
    /// própria semente da semente da partida e deste número. Mesma partida,
    /// mesmas decisões, mesmos resultados — em qualquer máquina, com qualquer
    /// intervalo entre as jogadas.
    /// </remarks>
    public int Passo { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private EstadoDaCarreira()
    {
    }

    public static EstadoDaCarreira Inicial(FichaDeInscricao ficha, LutadorCriado lutador)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        ArgumentNullException.ThrowIfNull(lutador);

        return new EstadoDaCarreira
        {
            Idade = ficha.IdadeInicial,
            Categoria = ficha.CategoriaDePeso,
            Atributos = lutador.Atributos,
            Etapa = EtapaDaCarreira.CircuitoRegional,
            OverallMaximo = lutador.Overall
        };
    }

    /// <summary>Overall do lutador com os atributos de agora.</summary>
    public decimal OverallAtual => CalculadoraDeOverall.Calcular(Atributos);

    public EstiloDeLuta Estilo => IdentificadorDeEstilo.Identificar(Atributos);

    /// <summary>Consome um passo e devolve o número que ele ocupou.</summary>
    internal int AvancarPasso() => Passo++;

    /// <param name="pesoDaVitoria">
    /// Quanto esta vitória vale na fila do degrau. Bater alguém acima do próprio
    /// nível conta por mais de uma luta — é o que faz aceitar a oferta perigosa
    /// ser uma decisão, e não só um risco.
    /// </param>
    internal void RegistrarResultado(
        ResultadoDaLuta resultado,
        MetodoDeEncerramento metodo,
        int pesoDaVitoria = 1)
    {
        RecusasSeguidas = 0;
        CompromissosNaTemporada++;

        switch (resultado)
        {
            case ResultadoDaLuta.Vitoria:
                VitoriasNaEtapa += pesoDaVitoria;
                DerrotasSeguidas = 0;
                break;

            case ResultadoDaLuta.Derrota:
                DerrotasSeguidas++;
                VitoriasNaEtapa = 0;

                if (metodo == MetodoDeEncerramento.Nocaute)
                {
                    NocautesSofridos++;
                    NocautesSofridosNoAno++;
                }

                break;

            case ResultadoDaLuta.Empate:
                VitoriasNaEtapa = 0;
                break;
        }
    }

    /// <summary>
    /// Registra que o jogador dispensou a rodada de ofertas.
    /// </summary>
    /// <remarks>
    /// Recusar não é de graça: gasta o mesmo espaço de calendário que uma luta
    /// gastaria e apaga o caso que o lutador vinha construindo para subir de
    /// degrau. Sem isso o jogador esperaria indefinidamente pela oferta perfeita,
    /// e escolher deixaria de ser uma decisão.
    /// </remarks>
    internal void RegistrarRecusa()
    {
        RecusasSeguidas++;
        CompromissosNaTemporada++;
        VitoriasNaEtapa = 0;
    }

    /// <summary>
    /// Toma a vaga de quem acabou de ser vencido.
    /// </summary>
    /// <remarks>
    /// Só melhora: bater alguém abaixo de você no ranking não te faz descer para
    /// a posição dele. A etapa passa a ser derivada da posição, porque no UFC
    /// quem diz onde você está é o número ao lado do seu nome, não um degrau
    /// abstrato.
    /// </remarks>
    internal void AssumirPosicaoNoRanking(int posicao)
    {
        if (PosicaoNoRanking is { } atual && atual <= posicao)
        {
            return;
        }

        PosicaoNoRanking = posicao;
        Etapa = EtapaPelaPosicao(posicao);
        VitoriasNaEtapa = 0;
    }

    /// <summary>Perder o cinturão devolve o ex-campeão ao primeiro lugar da fila.</summary>
    internal void CairParaODesafiante()
    {
        PosicaoNoRanking = 1;
        Etapa = EtapaDaCarreira.Top5;
        EhCampeao = false;
        VitoriasNaEtapa = 0;
    }

    /// <summary>
    /// A etapa que corresponde a uma posição do ranking. É o que mantém a escada
    /// antiga e o ranking real contando a mesma história.
    /// </summary>
    internal static EtapaDaCarreira EtapaPelaPosicao(int posicao) => posicao switch
    {
        TabelaDaDivisao.PosicaoDoCampeao => EtapaDaCarreira.Campeao,

        // Chegar a número um é, por definição, ser o próximo da fila. A luta
        // seguinte já é pelo cinturão.
        1 => EtapaDaCarreira.DisputaDeCinturao,

        <= 5 => EtapaDaCarreira.Top5,
        _ => EtapaDaCarreira.Top15
    };

    internal void SubirDeEtapa(EtapaDaCarreira destino)
    {
        Etapa = destino;
        VitoriasNaEtapa = 0;
    }

    internal void CairDeEtapa(EtapaDaCarreira destino)
    {
        Etapa = destino;
        VitoriasNaEtapa = 0;
        DerrotasSeguidas = 0;
        RecusasSeguidas = 0;
        VezesDispensado++;

        // Ser cortado apaga o número: ranking é da organização, e quem sai dela
        // sai da tabela.
        PosicaoNoRanking = null;
        EhCampeao = false;
    }

    internal void ConquistarCinturao()
    {
        Etapa = EtapaDaCarreira.Campeao;
        EhCampeao = true;
        VitoriasNaEtapa = 0;
        DefesasNaCategoria = 0;
        PosicaoNoRanking = TabelaDaDivisao.PosicaoDoCampeao;
    }

    internal void DefenderCinturao() => DefesasNaCategoria++;

    internal void PerderPosicaoDeTitulo()
    {
        Etapa = EtapaDaCarreira.Top5;
        EhCampeao = false;
        VitoriasNaEtapa = 0;
    }

    internal void MudarDeCategoria(CategoriaDePeso destino, int ajusteDeOverall)
    {
        Categoria = destino;
        Etapa = EtapaDaCarreira.Top15;
        EhCampeao = false;
        JaMudouDeCategoria = true;
        VitoriasNaEtapa = 0;
        DefesasNaCategoria = 0;
        AjusteDeOverallDoAdversario = ajusteDeOverall;

        // Subir de peso reabre a fila do zero: o cinturão da divisão antiga não
        // vale número na nova. É o que torna o duplo campeonato uma conquista.
        PosicaoNoRanking = null;
    }

    /// <summary>Fecha o ano: zera o calendário, envelhece e reavalia o pico.</summary>
    internal void VirarOAno(Atributos atributosDoAnoSeguinte)
    {
        ArgumentNullException.ThrowIfNull(atributosDoAnoSeguinte);

        Atributos = atributosDoAnoSeguinte;
        NocautesSofridosNoAno = 0;
        CompromissosNaTemporada = 0;
        Idade++;

        OverallMaximo = Math.Max(OverallMaximo, OverallAtual);
    }

    /// <summary>
    /// Registra a lesão e cobra a sequela na hora.
    /// </summary>
    /// <remarks>
    /// A perda de atributo é aplicada de uma vez, aqui, e não guardada como uma
    /// penalidade temporária que alguém precisa lembrar de remover quando a
    /// lesão sarar. Penalidade viva é penalidade que um dia fica para trás.
    /// <para>
    /// É por isso que lesão dói de verdade: o afastamento passa, o ponto de
    /// velocidade não volta. Um lutador que se machucou três vezes chega aos 32
    /// anos sendo outro lutador.
    /// </para>
    /// </remarks>
    internal void Lesionar(Lesao lesao)
    {
        ArgumentNullException.ThrowIfNull(lesao);

        LesaoAtual = lesao;
        LesoesSofridas++;

        var sequela = Domain.Lesao.SequelaDe(lesao.Gravidade);
        if (sequela == 0)
        {
            return;
        }

        Atributos = Atributos.ComAjustes(new Dictionary<Habilidade, int>
        {
            [Domain.Lesao.HabilidadeAfetadaPor(lesao.Tipo)] = -sequela
        });
    }

    /// <summary>
    /// Passa um compromisso do calendário em tratamento.
    /// </summary>
    /// <remarks>
    /// Recuperar-se gasta calendário como uma luta gastaria, mas <b>não</b>
    /// conta como recusa: quem está machucado não está fugindo de ninguém, e a
    /// organização não rescinde contrato por isso. Também não apaga o caso que
    /// o lutador vinha construindo — a lesão já cobrou o bastante.
    /// </remarks>
    /// <returns>Verdadeiro quando a lesão sarou e o lutador volta à ativa.</returns>
    internal bool TratarLesao()
    {
        if (LesaoAtual is not { } lesao)
        {
            return false;
        }

        CompromissosNaTemporada++;
        lesao.Tratar();

        if (!lesao.Sarou)
        {
            return false;
        }

        LesaoAtual = null;

        return true;
    }

    /// <summary>
    /// Grava o que o campo de treino produziu, antes de a luta acontecer.
    /// </summary>
    /// <remarks>
    /// Separado de <see cref="VirarOAno"/> porque são coisas diferentes: a
    /// virada do ano é o que o tempo faz com o lutador, e o camp é o que ele
    /// faz consigo mesmo. Um acontece com ele; o outro ele escolhe.
    /// </remarks>
    internal void AplicarCamp(Atributos atributosDepoisDoCamp)
    {
        ArgumentNullException.ThrowIfNull(atributosDepoisDoCamp);

        Atributos = atributosDepoisDoCamp;
        OverallMaximo = Math.Max(OverallMaximo, OverallAtual);
    }
}
