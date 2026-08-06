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
    }

    internal void ConquistarCinturao()
    {
        Etapa = EtapaDaCarreira.Campeao;
        EhCampeao = true;
        VitoriasNaEtapa = 0;
        DefesasNaCategoria = 0;
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
        Etapa = EtapaDaCarreira.Top5;
        EhCampeao = false;
        JaMudouDeCategoria = true;
        VitoriasNaEtapa = 0;
        DefesasNaCategoria = 0;
        AjusteDeOverallDoAdversario = ajusteDeOverall;
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
}
