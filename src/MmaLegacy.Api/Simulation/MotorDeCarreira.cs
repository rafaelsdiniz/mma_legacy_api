using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Conduz a carreira do lutador, uma decisão de cada vez.
/// </summary>
/// <remarks>
/// O motor não simula mais a vida inteira em um laço. Ele expõe os movimentos
/// que o jogador pode fazer — <see cref="Aceitar"/>, <see cref="Recusar"/>,
/// <see cref="Aposentar"/> — e, depois de cada um, arruma a mesa para o
/// seguinte: move o lutador na escada, cobra o preço do tempo, decide se a
/// organização ainda o quer e põe novas ofertas à frente dele.
/// <para>
/// <b>Determinismo sem sequência viva.</b> Antes havia um <see cref="Sorteio"/>
/// só, criado no início da simulação e consumido do começo ao fim. Com a
/// carreira jogada em requisições separadas essa sequência não sobrevive entre
/// uma jogada e outra, então cada passo deriva a própria semente da semente da
/// partida, do número do passo e da finalidade do sorteio. Três finalidades
/// distintas garantem que a geração das ofertas não ande de mãos dadas com o
/// resultado da luta nem com a evolução dos atributos.
/// </para>
/// <para>
/// <see cref="Simular"/> continua existindo e agora é apenas um jogador
/// automático: ele aceita a luta mais equilibrada de cada rodada até a
/// aposentadoria. É o que sustenta o "simular o resto da carreira" e os testes
/// de balanceamento, que precisam de milhares de carreiras completas.
/// </para>
/// </remarks>
public sealed class MotorDeCarreira(MotorDeLuta motorDeLuta, GeradorDeAdversarios geradorDeAdversarios)
{
    /// <summary>
    /// Quanto cada oferta da rodada foge da faixa normal do degrau. A primeira é
    /// sempre a segura e a última, a perigosa — é o eixo da decisão do jogador.
    /// </summary>
    private static readonly Dictionary<int, IReadOnlyList<int>> DesviosDeOverallPorOferta = new()
    {
        [1] = [0],
        [2] = [-4, 5],
        [3] = [-4, 0, 6]
    };

    private const int RoundsDeLutaComum = 3;
    private const int RoundsDeLutaDeCinturao = 5;

    /// <summary>Derrotas seguidas que fazem um veterano pendurar as luvas.</summary>
    private const int DerrotasQueAposentamVeterano = 2;

    /// <summary>
    /// Quanto o adversário precisa estar acima do lutador para a vitória contar
    /// dobrado na fila do degrau.
    /// </summary>
    private const int VantagemQueValeDobro = 3;

    /// <summary>Defesas de cinturão necessárias antes de tentar uma segunda categoria.</summary>
    private const int DefesasParaMudarDeCategoria = 3;

    /// <summary>Depois disso não sobra tempo para reconstruir tudo em outra divisão.</summary>
    private const int IdadeMaximaParaMudarDeCategoria = 33;

    /// <summary>Quanto a nova divisão é mais dura no mesmo degrau.</summary>
    private const int AjusteDeOverallPorSubirDeCategoria = 3;

    /// <summary>Idade em que todo lutador se aposenta, aconteça o que acontecer.</summary>
    private const int IdadeLimiteDeCarreira = 40;

    /// <summary>Teto de lutas, apenas como trava de segurança.</summary>
    private const int MaximoDeLutas = 60;

    private const int IdadeDeVeterano = 36;
    private const int IdadeDeCorpoCastigado = 34;
    private const int NocautesQueEncerramCarreira = 3;
    private const int IdadeParaDesistirSemResultado = 30;
    private const int LutasMinimasAntesDeDesistir = 10;
    private const int VitoriasMinimasParaSeguir = 5;

    // Salgam a semente de cada passo para que os três sorteios de uma mesma
    // decisão sejam sequências independentes.
    private const int FinalidadeDaLuta = 1;
    private const int FinalidadeDaEvolucao = 2;
    private const int FinalidadeDasOfertas = 3;

    /// <summary>
    /// Estreia o lutador: cria a carreira e põe a primeira luta na mesa.
    /// </summary>
    /// <remarks>
    /// A carreira volta solta, sem ser presa à partida. Quem faz esse vínculo é
    /// o serviço, que também é quem persiste — o motor não conhece banco.
    /// </remarks>
    public Carreira Iniciar(Partida partida, RankingDoJogo ranking)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(ranking);

        var lutador = partida.ExigirLutadorMontado();
        var carreira = Carreira.Iniciar(partida.Id, partida.Ficha, lutador);

        ColocarOfertasNaMesa(partida, carreira, ranking);

        return carreira;
    }

    /// <summary>
    /// O jogador aceita uma das ofertas: a luta acontece e a carreira reage a
    /// ela.
    /// </summary>
    /// <param name="indiceDaOferta">Índice da oferta escolhida, começando em 1.</param>
    public PassoDaCarreira Aceitar(
        Partida partida,
        Carreira carreira,
        RankingDoJogo ranking,
        int indiceDaOferta)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(carreira);
        ArgumentNullException.ThrowIfNull(ranking);

        var estado = carreira.Estado;
        var oferta = carreira.ExigirOferta(indiceDaOferta);
        var passo = estado.AvancarPasso();
        var eventos = new List<EventoDaCarreira>();

        var adversario = PerfilDeCombate.Montar(oferta.Adversario, oferta.AtributosDoAdversario);
        var desfecho = motorDeLuta.Simular(
            PerfilDeCombate.Montar(partida.Ficha.Nome, estado.Atributos),
            adversario,
            oferta.RoundsProgramados,
            Sortear(partida, passo, FinalidadeDaLuta));

        var luta = new LutaDaCarreira(
            ordem: carreira.TotalDeLutas + 1,
            idade: estado.Idade,
            adversario: adversario.Nome,
            overallDoAdversario: adversario.Overall,
            estiloDoAdversario: adversario.Estilo,
            organizacao: oferta.Organizacao,
            categoria: oferta.Categoria,
            disputaDeCinturao: oferta.DisputaDeCinturao,
            defesaDeCinturao: oferta.DefesaDeCinturao,
            roundsProgramados: oferta.RoundsProgramados,
            resultado: desfecho.Resultado,
            metodo: desfecho.Metodo,
            roundDoEncerramento: desfecho.RoundDoEncerramento);

        var pesoDaVitoria = PesoDaVitoria(estado, oferta);

        carreira.LimparOfertas();
        carreira.RegistrarLuta(luta);
        estado.RegistrarResultado(desfecho.Resultado, desfecho.Metodo, pesoDaVitoria);

        AvancarNaEscada(estado, oferta, desfecho.Resultado, eventos);
        ArrumarAMesaParaOProximoPasso(partida, carreira, ranking, passo, eventos);

        return new PassoDaCarreira(luta, desfecho, eventos);
    }

    /// <summary>
    /// O jogador recusa a rodada inteira de ofertas e fica parado.
    /// </summary>
    /// <remarks>
    /// A recusa consome o mesmo espaço de calendário que uma luta consumiria e
    /// apaga o caso que o lutador vinha construindo para subir de degrau. Três
    /// recusas seguidas e a organização o dispensa.
    /// </remarks>
    public PassoDaCarreira Recusar(Partida partida, Carreira carreira, RankingDoJogo ranking)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(carreira);
        ArgumentNullException.ThrowIfNull(ranking);

        RegraDeNegocioException.Se(
            carreira.Encerrada,
            "Esta carreira já foi encerrada.");

        RegraDeNegocioException.Se(
            carreira.Ofertas.Count == 0,
            "Não há nenhuma oferta de luta na mesa para recusar.");

        var estado = carreira.Estado;
        var passo = estado.AvancarPasso();
        var eventos = new List<EventoDaCarreira> { EventoDaCarreira.FicouInativo };

        carreira.LimparOfertas();
        estado.RegistrarRecusa();

        ArrumarAMesaParaOProximoPasso(partida, carreira, ranking, passo, eventos);

        return new PassoDaCarreira(null, null, eventos);
    }

    /// <summary>O jogador pendura as luvas por vontade própria, no auge ou não.</summary>
    public PassoDaCarreira Aposentar(Partida partida, Carreira carreira)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(carreira);

        var eventos = new List<EventoDaCarreira>();
        EncerrarCarreira(partida, carreira, MotivoDoEncerramento.EscolhaDoLutador, eventos);

        return new PassoDaCarreira(null, null, eventos);
    }

    /// <summary>
    /// Joga a carreira sozinho até o fim, aceitando sempre a luta mais
    /// equilibrada de cada rodada.
    /// </summary>
    /// <remarks>
    /// É o "simular o resto" de quem cansou no meio do caminho, e é também como
    /// os testes de balanceamento produzem carreiras inteiras. Escolher a oferta
    /// de overall mais próximo do próprio lutador imita um empresário sensato:
    /// nem só luta fácil, que não leva a lugar nenhum, nem só luta impossível.
    /// </remarks>
    public PassoDaCarreira SimularOResto(Partida partida, Carreira carreira, RankingDoJogo ranking)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(carreira);
        ArgumentNullException.ThrowIfNull(ranking);

        var eventos = new List<EventoDaCarreira>();
        LutaDaCarreira? ultimaLuta = null;

        while (!carreira.Encerrada && carreira.Ofertas.Count > 0)
        {
            var passo = Aceitar(
                partida,
                carreira,
                ranking,
                EscolherOfertaMaisEquilibrada(carreira).Indice);

            ultimaLuta = passo.Luta ?? ultimaLuta;
            eventos.AddRange(passo.Eventos);
        }

        return new PassoDaCarreira(ultimaLuta, null, eventos);
    }

    /// <summary>
    /// Estreia e joga a carreira inteira de uma vez.
    /// </summary>
    /// <param name="ranking">
    /// Quando omitido, a carreira acontece só contra adversários fictícios. É
    /// assim que os testes de balanceamento medem milhares de carreiras sem
    /// precisar de banco.
    /// </param>
    public Carreira Simular(Partida partida, RankingDoJogo? ranking = null)
    {
        ArgumentNullException.ThrowIfNull(partida);

        var tabelas = ranking ?? RankingDoJogo.Vazio;
        var carreira = Iniciar(partida, tabelas);

        SimularOResto(partida, carreira, tabelas);

        return carreira;
    }

    /// <summary>
    /// Tudo que acontece depois de uma decisão, na ordem em que a vida cobra:
    /// primeiro a organização decide se ainda quer o lutador, depois o
    /// calendário decide se o ano virou, e por fim o corpo decide se ainda dá.
    /// </summary>
    private void ArrumarAMesaParaOProximoPasso(
        Partida partida,
        Carreira carreira,
        RankingDoJogo ranking,
        int passo,
        List<EventoDaCarreira> eventos)
    {
        var motivoDaDispensa = AvaliarDispensa(carreira.Estado, eventos);

        if (motivoDaDispensa is null)
        {
            FecharTemporadaSeForOCaso(partida, carreira, passo, eventos);
        }

        if ((motivoDaDispensa ?? MotivoParaAposentar(carreira)) is { } motivo)
        {
            EncerrarCarreira(partida, carreira, motivo, eventos);
            return;
        }

        ColocarOfertasNaMesa(partida, carreira, ranking);
    }

    /// <summary>
    /// Decide se a organização rescinde o contrato.
    /// </summary>
    /// <returns>
    /// O motivo do fim da carreira quando não há degrau abaixo para onde cair —
    /// ser cortado no circuito regional é o fim da linha, não um recomeço.
    /// </returns>
    private static MotivoDoEncerramento? AvaliarDispensa(
        EstadoDaCarreira estado,
        List<EventoDaCarreira> eventos)
    {
        var foiCortado =
            estado.DerrotasSeguidas >= RegrasDaCarreira.DerrotasParaSerDispensado ||
            estado.RecusasSeguidas >= RegrasDaCarreira.RecusasParaSerDispensado;

        if (!foiCortado)
        {
            return null;
        }

        eventos.Add(EventoDaCarreira.Dispensado);

        if (estado.Etapa == EtapaDaCarreira.CircuitoRegional)
        {
            return MotivoDoEncerramento.SemContrato;
        }

        estado.CairDeEtapa(estado.Etapa - 1);
        eventos.Add(EventoDaCarreira.Rebaixado);

        return null;
    }

    /// <summary>Fecha o ano quando o calendário do degrau se esgota.</summary>
    private static void FecharTemporadaSeForOCaso(
        Partida partida,
        Carreira carreira,
        int passo,
        List<EventoDaCarreira> eventos)
    {
        var estado = carreira.Estado;

        if (estado.CompromissosNaTemporada < RegrasDaCarreira.CompromissosPorTemporada(estado.Etapa))
        {
            return;
        }

        if (AvaliarMudancaDeCategoria(estado))
        {
            eventos.Add(EventoDaCarreira.MudouDeCategoria);
        }

        estado.VirarOAno(CurvaDeEvolucao.AplicarAno(
            estado.Atributos,
            partida.ExigirLutadorMontado().Atributos,
            estado.Idade,
            estado.NocautesSofridosNoAno,
            Sortear(partida, passo, FinalidadeDaEvolucao)));

        eventos.Add(EventoDaCarreira.AnoVirado);
    }

    /// <summary>
    /// Move o lutador na escala de degraus conforme o resultado. Vitórias
    /// acumulam até a promoção; qualquer tropeço zera o progresso.
    /// </summary>
    private static void AvancarNaEscada(
        EstadoDaCarreira estado,
        OfertaDeLuta oferta,
        ResultadoDaLuta resultado,
        List<EventoDaCarreira> eventos)
    {
        switch (resultado)
        {
            case ResultadoDaLuta.Vitoria:
                PromoverAposVitoria(estado, oferta, eventos);
                break;

            case ResultadoDaLuta.Derrota:
                RebaixarAposDerrota(estado, eventos);
                break;

            case ResultadoDaLuta.Empate:
                // Empate não muda ninguém de lugar: apenas consome um
                // compromisso da temporada.
                break;
        }
    }

    private static void PromoverAposVitoria(
        EstadoDaCarreira estado,
        OfertaDeLuta oferta,
        List<EventoDaCarreira> eventos)
    {
        // Vitória sobre alguém do ranking: a posição dele passa a ser sua. É
        // isso que substitui a contagem abstrata de vitórias por degrau assim
        // que o jogador chega ao UFC.
        if (oferta.PosicaoDoAdversario is { } vaga)
        {
            PromoverPeloRanking(estado, vaga, eventos);
            return;
        }

        // Sem ranking real por perto — circuito regional, LFA, ou uma simulação
        // sem acervo carregado —, a escada antiga continua valendo.
        switch (estado.Etapa)
        {
            case EtapaDaCarreira.DisputaDeCinturao:
                estado.ConquistarCinturao();
                eventos.Add(EventoDaCarreira.CinturaoConquistado);
                break;

            case EtapaDaCarreira.Campeao:
                estado.DefenderCinturao();
                eventos.Add(EventoDaCarreira.CinturaoDefendido);
                break;

            default:
                if (estado.VitoriasNaEtapa >= RegrasDaCarreira.VitoriasParaSubir(estado.Etapa))
                {
                    var destino = estado.Etapa + 1;
                    estado.SubirDeEtapa(destino);

                    eventos.Add(destino == EtapaDaCarreira.DisputaDeCinturao
                        ? EventoDaCarreira.DisputaDeCinturaoMarcada
                        : EventoDaCarreira.Promovido);
                }

                break;
        }
    }

    private static void PromoverPeloRanking(
        EstadoDaCarreira estado,
        int vaga,
        List<EventoDaCarreira> eventos)
    {
        if (estado.EhCampeao)
        {
            estado.DefenderCinturao();
            eventos.Add(EventoDaCarreira.CinturaoDefendido);
            return;
        }

        if (vaga == TabelaDaDivisao.PosicaoDoCampeao)
        {
            estado.ConquistarCinturao();
            eventos.Add(EventoDaCarreira.CinturaoConquistado);
            return;
        }

        var estreouNoRanking = !estado.EstaRanqueado;
        var posicaoAnterior = estado.PosicaoNoRanking;

        estado.AssumirPosicaoNoRanking(vaga);

        if (estreouNoRanking || estado.PosicaoNoRanking < posicaoAnterior)
        {
            eventos.Add(EventoDaCarreira.Promovido);
        }

        // Chegar a número um é ganhar o direito de desafiar. A luta seguinte já
        // é pelo cinturão, e o jogador merece ser avisado disso.
        if (estado.Etapa == EtapaDaCarreira.DisputaDeCinturao)
        {
            eventos.Add(EventoDaCarreira.DisputaDeCinturaoMarcada);
        }
    }

    /// <summary>
    /// Perder o cinturão devolve o lutador ao topo do ranking, não ao começo. A
    /// queda de degrau fora do título é assunto da dispensa, não da derrota
    /// isolada.
    /// </summary>
    private static void RebaixarAposDerrota(EstadoDaCarreira estado, List<EventoDaCarreira> eventos)
    {
        switch (estado.Etapa)
        {
            case EtapaDaCarreira.Campeao:
                estado.CairParaODesafiante();
                eventos.Add(EventoDaCarreira.CinturaoPerdido);
                break;

            case EtapaDaCarreira.DisputaDeCinturao:
                // Perder a disputa não custa o número um: custa a vez. O
                // desafiante derrotado volta para a fila, ainda no topo dela.
                estado.CairParaODesafiante();
                break;
        }
    }

    /// <summary>
    /// Um campeão consolidado e ainda jovem tenta o segundo cinturão. Ele
    /// abandona o título atual e reentra na nova divisão como top 5 — subir de
    /// peso não vem com atalho.
    /// </summary>
    private static bool AvaliarMudancaDeCategoria(EstadoDaCarreira estado)
    {
        if (estado.JaMudouDeCategoria ||
            !estado.EhCampeao ||
            estado.DefesasNaCategoria < DefesasParaMudarDeCategoria ||
            estado.Idade > IdadeMaximaParaMudarDeCategoria ||
            Categorias.ProximaAcima(estado.Categoria) is not { } categoriaAcima)
        {
            return false;
        }

        estado.MudarDeCategoria(categoriaAcima, AjusteDeOverallPorSubirDeCategoria);

        return true;
    }

    private void ColocarOfertasNaMesa(Partida partida, Carreira carreira, RankingDoJogo ranking)
    {
        var sorteio = Sortear(partida, carreira.Estado.Passo, FinalidadeDasOfertas);
        var tabela = ranking.Da(carreira.Estado.Categoria);

        carreira.ReceberOfertas(GerarOfertas(carreira, tabela, sorteio));
    }

    private IReadOnlyList<OfertaDeLuta> GerarOfertas(
        Carreira carreira,
        TabelaDaDivisao tabela,
        Sorteio sorteio)
    {
        var estado = carreira.Estado;

        // Da grande organização para cima o adversário é gente de verdade: o
        // ranking da divisão é a escada, e enfrentar o décimo segundo colocado
        // significa alguma coisa que enfrentar um nome inventado nunca vai
        // significar.
        return EstaNaGrandeOrganizacao(estado.Etapa) && !tabela.EstaVazia
            ? GerarOfertasDoRanking(carreira, tabela)
            : GerarOfertasFicticias(carreira, sorteio);
    }

    /// <summary>
    /// Monta a rodada com atletas reais da divisão, escolhidos a partir de onde
    /// o jogador está no ranking.
    /// </summary>
    private static IReadOnlyList<OfertaDeLuta> GerarOfertasDoRanking(
        Carreira carreira,
        TabelaDaDivisao tabela)
    {
        var estado = carreira.Estado;

        // Campeão defende contra o primeiro da fila; quem venceu o número um
        // desafia o campeão. Nos dois casos a luta é uma só, e não se escolhe.
        var alvos = estado.Etapa switch
        {
            EtapaDaCarreira.Campeao => [1],
            EtapaDaCarreira.DisputaDeCinturao => [TabelaDaDivisao.PosicaoDoCampeao],
            _ => tabela.AlvosDe(estado.PosicaoNoRanking, RegrasDaCarreira.OfertasNaMesa(estado.Etapa))
        };

        var ofertas = new List<OfertaDeLuta>(alvos.Count);

        foreach (var alvo in alvos)
        {
            if (tabela.Em(alvo) is not { } adversario)
            {
                continue;
            }

            var valendoCinturao = alvo == TabelaDaDivisao.PosicaoDoCampeao || estado.EhCampeao;

            ofertas.Add(new OfertaDeLuta(
                indice: ofertas.Count + 1,
                adversario: adversario.Nome,
                cartelDoAdversario: string.Empty,
                atributosDoAdversario: adversario.Atributos,
                organizacao: NivelDaOrganizacao.GrandeOrganizacao,
                categoria: estado.Categoria,
                disputaDeCinturao: alvo == TabelaDaDivisao.PosicaoDoCampeao,
                defesaDeCinturao: estado.EhCampeao,
                roundsProgramados: valendoCinturao ? RoundsDeLutaDeCinturao : RoundsDeLutaComum,
                chamada: MontarChamadaDoRanking(estado, alvo),
                slugDoAdversario: adversario.Slug,
                posicaoDoAdversario: alvo));
        }

        return ofertas;
    }

    /// <summary>
    /// Monta a rodada com adversários inventados, para os degraus em que o
    /// jogador ainda não chega perto de ninguém ranqueado.
    /// </summary>
    private IReadOnlyList<OfertaDeLuta> GerarOfertasFicticias(Carreira carreira, Sorteio sorteio)
    {
        var estado = carreira.Estado;
        var disputaDeCinturao = estado.Etapa == EtapaDaCarreira.DisputaDeCinturao;
        var defesaDeCinturao = estado.Etapa == EtapaDaCarreira.Campeao;
        var valendoCinturao = disputaDeCinturao || defesaDeCinturao;

        var desvios = DesviosDeOverallPorOferta[RegrasDaCarreira.OfertasNaMesa(estado.Etapa)];
        var ofertas = new List<OfertaDeLuta>(desvios.Count);

        for (var posicao = 0; posicao < desvios.Count; posicao++)
        {
            var perfil = geradorDeAdversarios.Gerar(
                estado.Etapa,
                estado.AjusteDeOverallDoAdversario + desvios[posicao],
                sorteio);

            ofertas.Add(new OfertaDeLuta(
                indice: posicao + 1,
                adversario: perfil.Nome,
                cartelDoAdversario: geradorDeAdversarios.GerarCartel(estado.Etapa, sorteio),
                atributosDoAdversario: perfil.Atributos,
                organizacao: RegrasDaCarreira.OrganizacaoDe(estado.Etapa),
                categoria: estado.Categoria,
                disputaDeCinturao: disputaDeCinturao,
                defesaDeCinturao: defesaDeCinturao,
                roundsProgramados: valendoCinturao ? RoundsDeLutaDeCinturao : RoundsDeLutaComum,
                chamada: MontarChamada(carreira, disputaDeCinturao, defesaDeCinturao, desvios[posicao])));
        }

        return ofertas;
    }

    private static bool EstaNaGrandeOrganizacao(EtapaDaCarreira etapa) =>
        etapa >= EtapaDaCarreira.GrandeOrganizacao;

    private static string MontarChamadaDoRanking(EstadoDaCarreira estado, int alvo)
    {
        if (estado.EhCampeao)
        {
            return "Defesa de cinturão contra o desafiante número 1";
        }

        if (alvo == TabelaDaDivisao.PosicaoDoCampeao)
        {
            var categoria = Categorias.NomeDeExibicao(estado.Categoria).ToLowerInvariant();

            return $"Disputa do cinturão dos {categoria}s";
        }

        return estado.PosicaoNoRanking is null
            ? $"Vença o #{alvo} e entre no ranking"
            : $"Vença o #{alvo} e tome a posição dele";
    }

    /// <summary>Como a organização venderia esta luta no cartaz.</summary>
    private static string MontarChamada(
        Carreira carreira,
        bool disputaDeCinturao,
        bool defesaDeCinturao,
        int desvioDeOverall)
    {
        if (disputaDeCinturao)
        {
            var categoria = Categorias.NomeDeExibicao(carreira.Estado.Categoria).ToLowerInvariant();

            return $"Disputa do cinturão dos {categoria}s";
        }

        if (defesaDeCinturao)
        {
            return "Defesa de cinturão";
        }

        if (carreira.TotalDeLutas == 0)
        {
            return "Estreia no card preliminar";
        }

        return desvioDeOverall switch
        {
            < 0 => "Luta segura contra um nome abaixo do seu",
            > 0 => "Nome forte da divisão — vencer acelera a fila",
            _ => "Luta do card principal"
        };
    }

    /// <summary>
    /// A oferta de overall mais próximo do lutador. É o critério do jogador
    /// automático: a luta que mais mede alguma coisa.
    /// </summary>
    private static OfertaDeLuta EscolherOfertaMaisEquilibrada(Carreira carreira)
    {
        var overall = carreira.Estado.OverallAtual;

        return carreira.Ofertas
            .OrderBy(oferta => Math.Abs(oferta.OverallDoAdversario - overall))
            .ThenBy(oferta => oferta.Indice)
            .First();
    }

    /// <summary>
    /// Quanto esta vitória adianta na fila. Bater alguém acima do próprio nível
    /// vale por duas lutas — é o que compensa o risco de aceitar a oferta dura
    /// em vez da confortável.
    /// </summary>
    private static int PesoDaVitoria(EstadoDaCarreira estado, OfertaDeLuta oferta) =>
        oferta.OverallDoAdversario >= estado.OverallAtual + VantagemQueValeDobro ? 2 : 1;

    /// <summary>
    /// Motivos para pendurar as luvas, do mais duro ao mais melancólico: a idade
    /// limite, o corpo castigado, a sequência de derrotas do veterano e a
    /// carreira que nunca saiu do lugar.
    /// </summary>
    private static MotivoDoEncerramento? MotivoParaAposentar(Carreira carreira)
    {
        var estado = carreira.Estado;

        if (estado.Idade >= IdadeLimiteDeCarreira)
        {
            return MotivoDoEncerramento.IdadeLimite;
        }

        if (carreira.TotalDeLutas >= MaximoDeLutas)
        {
            return MotivoDoEncerramento.LimiteDeLutas;
        }

        if (estado.Idade >= IdadeDeCorpoCastigado && estado.NocautesSofridos >= NocautesQueEncerramCarreira)
        {
            return MotivoDoEncerramento.CorpoCastigado;
        }

        if (estado.Idade >= IdadeDeVeterano && estado.DerrotasSeguidas >= DerrotasQueAposentamVeterano)
        {
            return MotivoDoEncerramento.SequenciaDeDerrotas;
        }

        if (estado.Idade >= IdadeParaDesistirSemResultado &&
            carreira.TotalDeLutas >= LutasMinimasAntesDeDesistir &&
            carreira.Vitorias < VitoriasMinimasParaSeguir)
        {
            return MotivoDoEncerramento.SemResultados;
        }

        return null;
    }

    private static void EncerrarCarreira(
        Partida partida,
        Carreira carreira,
        MotivoDoEncerramento motivo,
        List<EventoDaCarreira> eventos)
    {
        carreira.Encerrar(motivo);
        CalculadoraDeLegado.Aplicar(carreira);

        // O motor também roda sobre partidas que nunca foram persistidas — os
        // testes de balanceamento simulam milhares delas sem tocar em banco. Só
        // mexe no status de quem de fato estreou pela via normal.
        if (partida.CarreiraEstaEmAndamento)
        {
            partida.EncerrarCarreira();
        }

        eventos.Add(EventoDaCarreira.CarreiraEncerrada);
    }

    /// <summary>
    /// Deriva a semente de um sorteio a partir da partida, do número do passo e
    /// da finalidade. Aritmética simples de propósito: precisa dar o mesmo
    /// número em qualquer máquina e em qualquer execução.
    /// </summary>
    private static Sorteio Sortear(Partida partida, int passo, int finalidade) =>
        new(unchecked((partida.SeedDaCarreira * 31) + (passo * 7919) + finalidade));
}
