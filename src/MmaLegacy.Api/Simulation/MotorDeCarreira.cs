using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Simula a trajetória inteira do lutador, da estreia à aposentadoria.
/// </summary>
/// <remarks>
/// A carreira é uma sequência de temporadas. Cada temporada tem um punhado de
/// lutas — mais no começo, menos no topo, como na vida real — e termina com um
/// ano a mais na conta e os atributos passados pela curva de evolução.
/// <para>
/// O motor não decide nada sobre lutas: ele só prepara o contexto (quem é o
/// adversário, quantos rounds, se vale cinturão), entrega ao
/// <see cref="MotorDeLuta"/> e reage ao resultado movendo o lutador para cima
/// ou para baixo na escala de <see cref="EtapaDaCarreira"/>.
/// </para>
/// </remarks>
public sealed class MotorDeCarreira(MotorDeLuta motorDeLuta, GeradorDeAdversarios geradorDeAdversarios)
{
    /// <summary>Quantas lutas por ano em cada degrau. No topo se luta menos.</summary>
    private static readonly Dictionary<EtapaDaCarreira, int> LutasPorTemporada = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = 4,
        [EtapaDaCarreira.OrganizacaoNacional] = 3,
        [EtapaDaCarreira.GrandeOrganizacao] = 2,
        [EtapaDaCarreira.Top15] = 2,
        [EtapaDaCarreira.Top5] = 2,
        [EtapaDaCarreira.DisputaDeCinturao] = 2,
        [EtapaDaCarreira.Campeao] = 2
    };

    /// <summary>Vitórias seguidas no degrau necessárias para subir ao próximo.</summary>
    private static readonly Dictionary<EtapaDaCarreira, int> VitoriasParaSubir = new()
    {
        [EtapaDaCarreira.CircuitoRegional] = 4,
        [EtapaDaCarreira.OrganizacaoNacional] = 4,
        [EtapaDaCarreira.GrandeOrganizacao] = 3,
        [EtapaDaCarreira.Top15] = 3,
        [EtapaDaCarreira.Top5] = 2
    };

    private const int RoundsDeLutaComum = 3;
    private const int RoundsDeLutaDeCinturao = 5;

    /// <summary>Derrotas seguidas que fazem o lutador cair um degrau.</summary>
    private const int DerrotasParaCair = 2;

    /// <summary>Defesas de cinturão necessárias antes de tentar uma segunda categoria.</summary>
    private const int DefesasParaMudarDeCategoria = 3;

    /// <summary>Depois disso não sobra tempo para reconstruir tudo em outra divisão.</summary>
    private const int IdadeMaximaParaMudarDeCategoria = 33;

    /// <summary>Quanto a nova divisão é mais dura no mesmo degrau.</summary>
    private const int AjusteDeOverallPorSubirDeCategoria = 3;

    /// <summary>Idade em que todo lutador se aposenta, aconteça o que acontecer.</summary>
    private const int IdadeLimiteDeCarreira = 40;

    /// <summary>Teto de lutas, apenas como trava de segurança do laço.</summary>
    private const int MaximoDeLutas = 60;

    private const int IdadeDeVeterano = 36;
    private const int IdadeDeCorpoCastigado = 34;
    private const int NocautesQueEncerramCarreira = 3;
    private const int IdadeParaDesistirSemResultado = 30;
    private const int LutasMinimasAntesDeDesistir = 10;
    private const int VitoriasMinimasParaSeguir = 5;

    public Carreira Simular(Partida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);

        var lutador = partida.ExigirLutadorMontado();
        var sorteio = new Sorteio(partida.SeedDaCarreira);
        var carreira = Carreira.Iniciar(partida.Id, partida.Ficha.IdadeInicial, partida.Ficha.CategoriaDePeso);
        var estado = new EstadoDaCarreira(partida.Ficha, lutador);

        // Laço do-while: todo lutador disputa ao menos uma temporada, mesmo
        // estreando na idade máxima permitida pela ficha.
        do
        {
            DisputarTemporada(carreira, estado, sorteio);
            EncerrarTemporada(estado, sorteio);
        }
        while (!DeveAposentar(estado));

        carreira.Encerrar(estado.Idade, estado.OverallMaximo);
        CalculadoraDeLegado.Aplicar(carreira);

        return carreira;
    }

    private void DisputarTemporada(Carreira carreira, EstadoDaCarreira estado, Sorteio sorteio)
    {
        var lutasDoAno = LutasPorTemporada[estado.Etapa];

        for (var luta = 0; luta < lutasDoAno && estado.TotalDeLutas < MaximoDeLutas; luta++)
        {
            DisputarLuta(carreira, estado, sorteio);
        }
    }

    private void DisputarLuta(Carreira carreira, EstadoDaCarreira estado, Sorteio sorteio)
    {
        var disputaDeCinturao = estado.Etapa == EtapaDaCarreira.DisputaDeCinturao;
        var defesaDeCinturao = estado.Etapa == EtapaDaCarreira.Campeao;
        var roundsProgramados = disputaDeCinturao || defesaDeCinturao
            ? RoundsDeLutaDeCinturao
            : RoundsDeLutaComum;

        var adversario = geradorDeAdversarios.Gerar(estado.Etapa, estado.AjusteDeOverallDoAdversario, sorteio);
        var protagonista = PerfilDeCombate.Montar(estado.Nome, estado.Atributos);

        var desfecho = motorDeLuta.Simular(protagonista, adversario, roundsProgramados, sorteio);

        carreira.RegistrarLuta(new LutaDaCarreira(
            ordem: estado.TotalDeLutas + 1,
            idade: estado.Idade,
            adversario: adversario.Nome,
            overallDoAdversario: adversario.Overall,
            estiloDoAdversario: adversario.Estilo,
            organizacao: DeterminarOrganizacao(estado.Etapa),
            categoria: estado.Categoria,
            disputaDeCinturao: disputaDeCinturao,
            defesaDeCinturao: defesaDeCinturao,
            roundsProgramados: roundsProgramados,
            resultado: desfecho.Resultado,
            metodo: desfecho.Metodo,
            roundDoEncerramento: desfecho.RoundDoEncerramento));

        estado.RegistrarResultado(desfecho);
        AvancarNaEscada(estado, desfecho.Resultado);
    }

    /// <summary>
    /// Move o lutador na escala de degraus conforme o resultado. Vitórias
    /// acumulam até a promoção; derrotas zeram o progresso e, se insistirem,
    /// rebaixam.
    /// </summary>
    private static void AvancarNaEscada(EstadoDaCarreira estado, ResultadoDaLuta resultado)
    {
        switch (resultado)
        {
            case ResultadoDaLuta.Vitoria:
                PromoverAposVitoria(estado);
                break;

            case ResultadoDaLuta.Derrota:
                RebaixarAposDerrota(estado);
                break;

            case ResultadoDaLuta.Empate:
                // Empate não muda nada de lugar: não promove nem rebaixa,
                // apenas consome uma luta da temporada.
                break;
        }
    }

    private static void PromoverAposVitoria(EstadoDaCarreira estado)
    {
        switch (estado.Etapa)
        {
            case EtapaDaCarreira.DisputaDeCinturao:
                estado.ConquistarCinturao();
                break;

            case EtapaDaCarreira.Campeao:
                estado.DefenderCinturao();
                break;

            default:
                if (estado.VitoriasNaEtapa >= VitoriasParaSubir[estado.Etapa])
                {
                    estado.SubirDeEtapa(estado.Etapa + 1);
                }

                break;
        }
    }

    private static void RebaixarAposDerrota(EstadoDaCarreira estado)
    {
        switch (estado.Etapa)
        {
            // Perder o cinturão devolve o lutador ao topo do ranking, não ao começo.
            case EtapaDaCarreira.Campeao:
            case EtapaDaCarreira.DisputaDeCinturao:
                estado.PerderPosicaoDeTitulo();
                break;

            default:
                if (estado.DerrotasSeguidas >= DerrotasParaCair && estado.Etapa > EtapaDaCarreira.CircuitoRegional)
                {
                    estado.CairDeEtapa(estado.Etapa - 1);
                }

                break;
        }
    }

    /// <summary>Fecha o ano: envelhece o lutador e reavalia a mudança de categoria.</summary>
    private static void EncerrarTemporada(EstadoDaCarreira estado, Sorteio sorteio)
    {
        AvaliarMudancaDeCategoria(estado);
        estado.Envelhecer(sorteio);
    }

    /// <summary>
    /// Um campeão consolidado e ainda jovem tenta o segundo cinturão. Ele
    /// abandona o título atual e reentra na nova divisão como top 5 — subir de
    /// peso não vem com atalho.
    /// </summary>
    private static void AvaliarMudancaDeCategoria(EstadoDaCarreira estado)
    {
        if (estado.JaMudouDeCategoria ||
            !estado.EhCampeao ||
            estado.DefesasNaCategoria < DefesasParaMudarDeCategoria ||
            estado.Idade > IdadeMaximaParaMudarDeCategoria)
        {
            return;
        }

        if (Categorias.ProximaAcima(estado.Categoria) is { } categoriaAcima)
        {
            estado.MudarDeCategoria(categoriaAcima, AjusteDeOverallPorSubirDeCategoria);
        }
    }

    private static NivelDaOrganizacao DeterminarOrganizacao(EtapaDaCarreira etapa) => etapa switch
    {
        EtapaDaCarreira.CircuitoRegional => NivelDaOrganizacao.CircuitoRegional,
        EtapaDaCarreira.OrganizacaoNacional => NivelDaOrganizacao.OrganizacaoNacional,
        _ => NivelDaOrganizacao.GrandeOrganizacao
    };

    /// <summary>
    /// Motivos para pendurar as luvas, do mais duro ao mais melancólico: a
    /// idade limite, o corpo castigado, a sequência de derrotas do veterano e a
    /// carreira que nunca saiu do lugar.
    /// </summary>
    private static bool DeveAposentar(EstadoDaCarreira estado) =>
        estado.Idade >= IdadeLimiteDeCarreira ||
        estado.TotalDeLutas >= MaximoDeLutas ||
        (estado.Idade >= IdadeDeVeterano && estado.DerrotasSeguidas >= DerrotasParaCair) ||
        (estado.Idade >= IdadeDeCorpoCastigado && estado.NocautesSofridos >= NocautesQueEncerramCarreira) ||
        (estado.Idade >= IdadeParaDesistirSemResultado &&
         estado.TotalDeLutas >= LutasMinimasAntesDeDesistir &&
         estado.Vitorias < VitoriasMinimasParaSeguir);

    /// <summary>
    /// Tudo que muda ao longo da carreira. Fica fora da entidade
    /// <see cref="Carreira"/> de propósito: é andaime da simulação, não
    /// informação que o jogador precise ver depois.
    /// </summary>
    private sealed class EstadoDaCarreira
    {
        /// <summary>
        /// Atributos com que o lutador saiu do draft. Guardados para servir de
        /// referência do teto de potencial em toda a carreira.
        /// </summary>
        private readonly Atributos _atributosDeEstreia;

        public EstadoDaCarreira(FichaDeInscricao ficha, LutadorCriado lutador)
        {
            Nome = ficha.Nome;
            Idade = ficha.IdadeInicial;
            Categoria = ficha.CategoriaDePeso;
            Atributos = lutador.Atributos;
            OverallMaximo = lutador.Overall;
            _atributosDeEstreia = lutador.Atributos;
        }

        public string Nome { get; }
        public int Idade { get; private set; }
        public Atributos Atributos { get; private set; }
        public CategoriaDePeso Categoria { get; private set; }
        public EtapaDaCarreira Etapa { get; private set; } = EtapaDaCarreira.CircuitoRegional;

        public decimal OverallMaximo { get; private set; }
        public int TotalDeLutas { get; private set; }
        public int Vitorias { get; private set; }
        public int VitoriasNaEtapa { get; private set; }
        public int DerrotasSeguidas { get; private set; }
        public int NocautesSofridos { get; private set; }
        public int DefesasNaCategoria { get; private set; }

        public bool EhCampeao { get; private set; }
        public bool JaMudouDeCategoria { get; private set; }
        public int AjusteDeOverallDoAdversario { get; private set; }

        private int _nocautesSofridosNoAno;

        public void RegistrarResultado(ResultadoDaLutaSimulada desfecho)
        {
            TotalDeLutas++;

            switch (desfecho.Resultado)
            {
                case ResultadoDaLuta.Vitoria:
                    Vitorias++;
                    VitoriasNaEtapa++;
                    DerrotasSeguidas = 0;
                    break;

                case ResultadoDaLuta.Derrota:
                    DerrotasSeguidas++;
                    VitoriasNaEtapa = 0;

                    if (desfecho.Metodo == MetodoDeEncerramento.Nocaute)
                    {
                        NocautesSofridos++;
                        _nocautesSofridosNoAno++;
                    }

                    break;

                case ResultadoDaLuta.Empate:
                    VitoriasNaEtapa = 0;
                    break;
            }
        }

        public void SubirDeEtapa(EtapaDaCarreira destino)
        {
            Etapa = destino;
            VitoriasNaEtapa = 0;
        }

        public void CairDeEtapa(EtapaDaCarreira destino)
        {
            Etapa = destino;
            VitoriasNaEtapa = 0;
            DerrotasSeguidas = 0;
        }

        public void ConquistarCinturao()
        {
            Etapa = EtapaDaCarreira.Campeao;
            EhCampeao = true;
            VitoriasNaEtapa = 0;
            DefesasNaCategoria = 0;
        }

        public void DefenderCinturao() => DefesasNaCategoria++;

        public void PerderPosicaoDeTitulo()
        {
            Etapa = EtapaDaCarreira.Top5;
            EhCampeao = false;
            VitoriasNaEtapa = 0;
        }

        public void MudarDeCategoria(CategoriaDePeso destino, int ajusteDeOverall)
        {
            Categoria = destino;
            Etapa = EtapaDaCarreira.Top5;
            EhCampeao = false;
            JaMudouDeCategoria = true;
            VitoriasNaEtapa = 0;
            DefesasNaCategoria = 0;
            AjusteDeOverallDoAdversario = ajusteDeOverall;
        }

        public void Envelhecer(Sorteio sorteio)
        {
            Atributos = CurvaDeEvolucao.AplicarAno(
                Atributos,
                _atributosDeEstreia,
                Idade,
                _nocautesSofridosNoAno,
                sorteio);

            _nocautesSofridosNoAno = 0;
            Idade++;

            OverallMaximo = Math.Max(OverallMaximo, CalculadoraDeOverall.Calcular(Atributos));
        }
    }
}
