using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// Simula uma luta round a round.
/// </summary>
/// <remarks>
/// O motor não compara overalls. Cada round resolve, nesta ordem, a troca em
/// pé, a tentativa de queda, a tentativa de finalização e a chance de nocaute —
/// e só então distribui os pontos. É isso que faz um wrestler de overall 84
/// vencer um nocauteador de overall 90 com defesa de queda ruim: o overall
/// nunca entra na conta.
/// <para>
/// Fadiga e dano acumulam entre os rounds. Um lutador de cardio baixo perde
/// eficiência ofensiva <b>e</b> defensiva conforme a luta avança, e quem já
/// apanhou fica mais fácil de nocautear — é o que faz lutas de cinco rounds
/// terem um perfil de resultado diferente das de três.
/// </para>
/// </remarks>
public sealed class MotorDeLuta
{
    /// <summary>Quanto da eficiência se perde com o lutador completamente exausto.</summary>
    private const double ImpactoDaFadiga = 0.30;

    /// <summary>Desgaste base de um round, antes do alívio dado pelo cardio.</summary>
    private const double CustoBaseDoRound = 0.24;

    /// <summary>Quanto um cardio nota 100 devolve do custo do round.</summary>
    private const double AlivioPorCardio = 0.16;

    /// <summary>Custo extra de um round em que se buscou queda.</summary>
    private const double CustoExtraDeQueda = 0.04;

    /// <summary>
    /// Custo extra de quem soca forte. Potência cansa: é o que impede o
    /// nocauteador puro de ser dominante também no quinto round.
    /// </summary>
    private const double CustoExtraPorPotencia = 0.03;

    /// <summary>
    /// Suaviza a conversão de "diferença de pontos" em probabilidade. Quanto
    /// menor, mais determinística fica a luta; quanto maior, mais imprevisível.
    /// </summary>
    private const double SuavizacaoDaProbabilidade = 10.0;

    /// <summary>Chance de nocaute por round entre dois lutadores equivalentes.</summary>
    private const double ChanceBaseDeNocaute = 0.065;

    /// <summary>Teto da chance de nocaute em um round, para nenhuma luta virar moeda viciada.</summary>
    private const double ChanceMaximaDeNocaute = 0.40;

    /// <summary>Potência considerada média ao calibrar a chance de nocaute.</summary>
    private const double PotenciaDeReferencia = 85.0;

    /// <summary>Quanto o dano já acumulado amplifica a chance de nocaute.</summary>
    private const double PesoDoDanoAcumulado = 0.9;

    /// <summary>Chance de finalização em um round com controle e jiu-jítsu equivalente.</summary>
    private const double ChanceBaseDeFinalizacao = 0.22;

    /// <summary>Vantagem em pé que o controle no chão vale na pontuação do round.</summary>
    private const double ValorDoControleNoRound = 8.0;

    /// <summary>Abaixo desta diferença o round é considerado equilibrado e ninguém pontua.</summary>
    private const double MargemDeRoundEquilibrado = 0.75;

    /// <summary>
    /// Diferença de efeito acumulado abaixo da qual uma decisão com rounds
    /// empatados vira empate de verdade, em vez de ser desempatada.
    /// </summary>
    private const double MargemDeDecisaoEmpatada = 3.0;

    /// <summary>Dano máximo que um único round pode causar.</summary>
    private const double DanoMaximoPorRound = 0.25;

    public ResultadoDaLutaSimulada Simular(
        PerfilDeCombate protagonista,
        PerfilDeCombate adversario,
        int roundsProgramados,
        Sorteio sorteio)
    {
        ArgumentNullException.ThrowIfNull(protagonista);
        ArgumentNullException.ThrowIfNull(adversario);
        ArgumentNullException.ThrowIfNull(sorteio);
        DadoInvalidoException.ExigirIntervalo(roundsProgramados, 1, 5, nameof(roundsProgramados));

        var euh = new EstadoDoLutador(protagonista);
        var ele = new EstadoDoLutador(adversario);
        var rounds = new List<RoundDaLuta>(roundsProgramados);

        for (var round = 1; round <= roundsProgramados; round++)
        {
            var minhaAcao = ResolverAcao(euh, ele, sorteio);
            var acaoDele = ResolverAcao(ele, euh, sorteio);

            // A ordem de resolução é sorteada a cada round. Resolver sempre o
            // protagonista primeiro daria a ele a primeira chance de finalizar
            // e de nocautear, um viés pequeno mas sistemático ao longo de uma
            // carreira inteira.
            var euResolvoPrimeiro = sorteio.Acontece(0.5);

            var desfecho = euResolvoPrimeiro
                ? ResolverEncerramento(euh, ele, minhaAcao, acaoDele, round, sorteio)
                : Inverter(ResolverEncerramento(ele, euh, acaoDele, minhaAcao, round, sorteio));

            if (desfecho is not null)
            {
                rounds.Add(RegistrarRound(
                    round,
                    euh,
                    ele,
                    minhaAcao,
                    acaoDele,
                    VencedorPeloDesfecho(desfecho.Resultado),
                    desfecho.Metodo));

                return desfecho with { Rounds = rounds };
            }

            var vencedorDoRound = PontuarRound(euh, ele, minhaAcao, acaoDele);
            AplicarDesgaste(euh, minhaAcao);
            AplicarDesgaste(ele, acaoDele);

            rounds.Add(RegistrarRound(round, euh, ele, minhaAcao, acaoDele, vencedorDoRound, encerramento: null));
        }

        return DecidirNosPontos(euh, ele, roundsProgramados) with { Rounds = rounds };
    }

    /// <summary>
    /// Fotografa o round: o que cada um tentou e como os dois chegaram ao fim
    /// dele. Nos rounds completos os números de fadiga e dano já são os de
    /// depois do desgaste; no round que encerra a luta são os do momento em que
    /// ela parou.
    /// </summary>
    private static RoundDaLuta RegistrarRound(
        int numero,
        EstadoDoLutador euh,
        EstadoDoLutador ele,
        AcaoDoRound minhaAcao,
        AcaoDoRound acaoDele,
        VencedorDoRound vencedor,
        MetodoDeEncerramento? encerramento) => new(
        numero,
        vencedor,
        minhaAcao.BuscouQueda,
        minhaAcao.Controlou,
        acaoDele.BuscouQueda,
        acaoDele.Controlou,
        EmPercentual(euh.Fadiga),
        EmPercentual(ele.Fadiga),
        EmPercentual(euh.Dano),
        EmPercentual(ele.Dano),
        encerramento);

    private static int EmPercentual(double fracao) =>
        (int)Math.Round(Math.Clamp(fracao, 0, 1) * 100, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Quem levou o round que acabou em finalização ou nocaute: quem terminou a
    /// luta. Não há cartão a preencher quando o juiz interrompe.
    /// </summary>
    private static VencedorDoRound VencedorPeloDesfecho(ResultadoDaLuta resultado) => resultado switch
    {
        ResultadoDaLuta.Vitoria => VencedorDoRound.Lutador,
        ResultadoDaLuta.Derrota => VencedorDoRound.Adversario,
        _ => VencedorDoRound.Nenhum
    };

    /// <summary>
    /// Calcula o que um lutador produziu no round: quanta ofensiva em pé, se
    /// buscou a queda e se conseguiu o controle.
    /// </summary>
    private static AcaoDoRound ResolverAcao(EstadoDoLutador atacante, EstadoDoLutador defensor, Sorteio sorteio)
    {
        var vantagem = MatchupDeEstilos.Vantagem(atacante.Perfil.Estilo, defensor.Perfil.Estilo);

        var ofensivaEmPe = PontuarOfensivaEmPe(atacante) * vantagem * sorteio.FatorDeSorte();
        var defesaEmPe = PontuarDefesaEmPe(defensor);
        var vantagemEmPe = ofensivaEmPe - defesaEmPe;

        var buscouQueda = sorteio.Acontece(CalcularPropensaoAQueda(atacante.Perfil.Atributos));
        var controlou = false;

        if (buscouQueda)
        {
            var ataqueDeQueda = PontuarAtaqueDeQueda(atacante) * vantagem * sorteio.FatorDeSorte();
            var defesaDeQueda = PontuarDefesaDeQueda(defensor);
            controlou = sorteio.Acontece(ConverterEmProbabilidade(ataqueDeQueda - defesaDeQueda));
        }

        return new AcaoDoRound(vantagemEmPe, buscouQueda, controlou);
    }

    /// <summary>
    /// Verifica se o round termina antes do tempo, primeiro por finalização
    /// (quem tem controle) e depois por nocaute.
    /// </summary>
    /// <returns>O desfecho, ou <c>null</c> se a luta continua.</returns>
    private static ResultadoDaLutaSimulada? ResolverEncerramento(
        EstadoDoLutador primeiro,
        EstadoDoLutador segundo,
        AcaoDoRound acaoDoPrimeiro,
        AcaoDoRound acaoDoSegundo,
        int round,
        Sorteio sorteio)
    {
        if (acaoDoPrimeiro.Controlou && TentarFinalizacao(primeiro, segundo, sorteio))
        {
            return new ResultadoDaLutaSimulada(ResultadoDaLuta.Vitoria, MetodoDeEncerramento.Finalizacao, round);
        }

        if (acaoDoSegundo.Controlou && TentarFinalizacao(segundo, primeiro, sorteio))
        {
            return new ResultadoDaLutaSimulada(ResultadoDaLuta.Derrota, MetodoDeEncerramento.Finalizacao, round);
        }

        if (TentarNocaute(primeiro, segundo, acaoDoPrimeiro, sorteio))
        {
            return new ResultadoDaLutaSimulada(ResultadoDaLuta.Vitoria, MetodoDeEncerramento.Nocaute, round);
        }

        if (TentarNocaute(segundo, primeiro, acaoDoSegundo, sorteio))
        {
            return new ResultadoDaLutaSimulada(ResultadoDaLuta.Derrota, MetodoDeEncerramento.Nocaute, round);
        }

        return null;
    }

    /// <summary>Troca a perspectiva do desfecho quando a resolução começou pelo adversário.</summary>
    private static ResultadoDaLutaSimulada? Inverter(ResultadoDaLutaSimulada? desfecho) => desfecho is null
        ? null
        : desfecho with
        {
            Resultado = desfecho.Resultado switch
            {
                ResultadoDaLuta.Vitoria => ResultadoDaLuta.Derrota,
                ResultadoDaLuta.Derrota => ResultadoDaLuta.Vitoria,
                _ => ResultadoDaLuta.Empate
            }
        };

    private static bool TentarFinalizacao(EstadoDoLutador atacante, EstadoDoLutador defensor, Sorteio sorteio)
    {
        var atributosAtacante = atacante.Perfil.Atributos;
        var atributosDefensor = defensor.Perfil.Atributos;

        var ataque =
            ((atributosAtacante[Habilidade.JiuJitsu] * 0.75) +
             (atributosAtacante[Habilidade.Wrestling] * 0.25))
            * atacante.Eficiencia
            * sorteio.FatorDeSorte();

        var defesa =
            ((atributosDefensor[Habilidade.JiuJitsu] * 0.65) +
             (atributosDefensor[Habilidade.InteligenciaDeLuta] * 0.20) +
             (atributosDefensor[Habilidade.Resistencia] * 0.15))
            * defensor.Eficiencia;

        // O fator 2 mantém a chance base quando os dois se equivalem: a
        // conversão devolve 0,5 no empate técnico.
        var chance = ChanceBaseDeFinalizacao * ConverterEmProbabilidade(ataque - defesa) * 2;
        return sorteio.Acontece(chance);
    }

    private static bool TentarNocaute(
        EstadoDoLutador atacante,
        EstadoDoLutador defensor,
        AcaoDoRound acao,
        Sorteio sorteio)
    {
        var chance =
            ChanceBaseDeNocaute
            * (atacante.Perfil.Atributos[Habilidade.Potencia] / PotenciaDeReferencia)
            * (ConverterEmProbabilidade(acao.VantagemEmPe) * 2)
            * (1 + (defensor.Dano * PesoDoDanoAcumulado));

        return sorteio.Acontece(Math.Min(chance, ChanceMaximaDeNocaute));
    }

    /// <summary>
    /// Decide quem levou o round e transfere dano para quem perdeu. Rounds
    /// muito parelhos não pontuam para ninguém, o que abre espaço para empates.
    /// </summary>
    /// <returns>Quem levou o round, para o registro round a round.</returns>
    private static VencedorDoRound PontuarRound(
        EstadoDoLutador euh,
        EstadoDoLutador ele,
        AcaoDoRound minhaAcao,
        AcaoDoRound acaoDele)
    {
        var meuEfeito = CalcularEfeito(minhaAcao);
        var efeitoDele = CalcularEfeito(acaoDele);
        var diferenca = meuEfeito - efeitoDele;

        euh.AcumularEfeito(meuEfeito);
        ele.AcumularEfeito(efeitoDele);

        if (Math.Abs(diferenca) < MargemDeRoundEquilibrado)
        {
            return VencedorDoRound.Nenhum;
        }

        var (vencedor, perdedor) = diferenca > 0 ? (euh, ele) : (ele, euh);
        vencedor.VencerRound();
        perdedor.AcumularDano(Math.Abs(diferenca), vencedor.Perfil.Atributos[Habilidade.Potencia]);

        return diferenca > 0 ? VencedorDoRound.Lutador : VencedorDoRound.Adversario;
    }

    private static double CalcularEfeito(AcaoDoRound acao) =>
        acao.VantagemEmPe + (acao.Controlou ? ValorDoControleNoRound : 0);

    private static void AplicarDesgaste(EstadoDoLutador lutador, AcaoDoRound acao)
    {
        var atributos = lutador.Perfil.Atributos;

        var custo =
            CustoBaseDoRound
            - (atributos[Habilidade.Cardio] / 100.0 * AlivioPorCardio)
            + (atributos[Habilidade.Potencia] / 100.0 * CustoExtraPorPotencia)
            + (acao.BuscouQueda ? CustoExtraDeQueda : 0);

        lutador.Cansar(custo);
    }

    /// <summary>
    /// Decide a luta nos cartões.
    /// </summary>
    /// <remarks>
    /// Rounds vencidos mandam. Se empatarem — o que acontece quando houve
    /// rounds equilibrados demais para pontuar —, desempata o efeito acumulado
    /// na luta inteira, que é como um juiz decide um round parelho olhando o
    /// conjunto. Só quando nem isso separa os dois sai um empate de verdade,
    /// como no MMA real, onde empate é raro mas existe.
    /// </remarks>
    private static ResultadoDaLutaSimulada DecidirNosPontos(
        EstadoDoLutador euh,
        EstadoDoLutador ele,
        int roundsProgramados)
    {
        var resultado = euh.RoundsVencidos.CompareTo(ele.RoundsVencidos) switch
        {
            > 0 => ResultadoDaLuta.Vitoria,
            < 0 => ResultadoDaLuta.Derrota,
            _ => DesempatarPeloConjunto(euh, ele)
        };

        return new ResultadoDaLutaSimulada(resultado, MetodoDeEncerramento.Decisao, roundsProgramados);
    }

    private static ResultadoDaLuta DesempatarPeloConjunto(EstadoDoLutador euh, EstadoDoLutador ele)
    {
        var diferenca = euh.EfeitoAcumulado - ele.EfeitoAcumulado;

        if (Math.Abs(diferenca) < MargemDeDecisaoEmpatada)
        {
            return ResultadoDaLuta.Empate;
        }

        return diferenca > 0 ? ResultadoDaLuta.Vitoria : ResultadoDaLuta.Derrota;
    }

    private static double PontuarOfensivaEmPe(EstadoDoLutador lutador)
    {
        var atributos = lutador.Perfil.Atributos;
        var bruta =
            (atributos[Habilidade.Striking] * 0.35) +
            (atributos[Habilidade.Potencia] * 0.25) +
            (atributos[Habilidade.Velocidade] * 0.20) +
            (atributos[Habilidade.InteligenciaDeLuta] * 0.20);

        return bruta * lutador.Eficiencia;
    }

    private static double PontuarDefesaEmPe(EstadoDoLutador lutador)
    {
        var atributos = lutador.Perfil.Atributos;
        var bruta =
            (atributos[Habilidade.Resistencia] * 0.40) +
            (atributos[Habilidade.Velocidade] * 0.30) +
            (atributos[Habilidade.InteligenciaDeLuta] * 0.30);

        return bruta * lutador.Eficiencia;
    }

    private static double PontuarAtaqueDeQueda(EstadoDoLutador lutador)
    {
        var atributos = lutador.Perfil.Atributos;
        var bruta =
            (atributos[Habilidade.Wrestling] * 0.70) +
            (atributos[Habilidade.InteligenciaDeLuta] * 0.15) +
            (atributos[Habilidade.Cardio] * 0.15);

        return bruta * lutador.Eficiencia;
    }

    private static double PontuarDefesaDeQueda(EstadoDoLutador lutador)
    {
        var atributos = lutador.Perfil.Atributos;
        var bruta =
            (atributos[Habilidade.Wrestling] * 0.60) +
            (atributos[Habilidade.Resistencia] * 0.20) +
            (atributos[Habilidade.InteligenciaDeLuta] * 0.20);

        return bruta * lutador.Eficiencia;
    }

    /// <summary>
    /// Com que frequência o lutador tenta a queda, deduzida do quanto ele é
    /// melhor de wrestling do que de luta em pé. Ninguém que soca bem passa a
    /// luta puxando perna, e nem o striker mais puro deixa de tentar uma queda
    /// de vez em quando.
    /// </summary>
    private static double CalcularPropensaoAQueda(Atributos atributos)
    {
        var inclinacao = (atributos[Habilidade.Wrestling] - atributos[Habilidade.Striking] + 20) / 60.0;
        return Math.Clamp(inclinacao, 0.10, 0.85);
    }

    /// <summary>
    /// Converte uma diferença de pontuação em probabilidade por curva
    /// logística: devolve 0,5 no empate técnico e se aproxima de 0 ou 1 nos
    /// extremos, sem nunca chegar lá. Garante que nenhuma luta seja impossível
    /// de vencer, por maior que seja a diferença de nível.
    /// </summary>
    private static double ConverterEmProbabilidade(double diferenca) =>
        1.0 / (1.0 + Math.Exp(-diferenca / SuavizacaoDaProbabilidade));

    /// <summary>O que um lutador produziu em um round.</summary>
    private readonly record struct AcaoDoRound(double VantagemEmPe, bool BuscouQueda, bool Controlou);

    /// <summary>
    /// Estado mutável de um lutador dentro de uma luta. Vive só durante a
    /// simulação: os atributos originais nunca são tocados.
    /// </summary>
    private sealed class EstadoDoLutador(PerfilDeCombate perfil)
    {
        public PerfilDeCombate Perfil { get; } = perfil;

        /// <summary>Cansaço acumulado, de 0 a 1.</summary>
        public double Fadiga { get; private set; }

        /// <summary>Castigo acumulado, de 0 a 1. Aumenta a vulnerabilidade ao nocaute.</summary>
        public double Dano { get; private set; }

        public int RoundsVencidos { get; private set; }

        /// <summary>Soma do efeito produzido em todos os rounds, usada para desempatar decisões.</summary>
        public double EfeitoAcumulado { get; private set; }

        /// <summary>Multiplicador aplicado a tudo que o lutador faz, corroído pela fadiga.</summary>
        public double Eficiencia => 1.0 - (Fadiga * ImpactoDaFadiga);

        public void VencerRound() => RoundsVencidos++;

        public void AcumularEfeito(double efeito) => EfeitoAcumulado += efeito;

        public void Cansar(double custo) => Fadiga = Math.Clamp(Fadiga + custo, 0, 1);

        public void AcumularDano(double diferencaDoRound, int potenciaDoAdversario)
        {
            var dano = Math.Min(diferencaDoRound / 100.0, DanoMaximoPorRound) * (potenciaDoAdversario / 100.0);
            Dano = Math.Clamp(Dano + dano, 0, 1);
        }
    }
}
