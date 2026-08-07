using FluentAssertions;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Simulation;
using MmaLegacy.Tests.Support;
using Xunit.Abstractions;

namespace MmaLegacy.Tests.Simulation;

/// <summary>
/// Guarda de regressão do balanceamento.
/// </summary>
/// <remarks>
/// Os outros testes verificam regras; este verifica <b>proporções</b>. Uma
/// mudança em qualquer constante do motor — chance de nocaute, custo de fadiga,
/// faixa de overall dos adversários, teto de evolução — reaparece aqui como uma
/// distribuição fora da faixa esperada, e não como uma carreira estranha que
/// ninguém percebeu.
/// <para>
/// As faixas são largas de propósito: elas existem para pegar mudança de
/// patamar, não para congelar o número exato. Rode com
/// <c>dotnet test --logger "console;verbosity=detailed"</c> para ver as
/// distribuições impressas e recalibrar com dados na mão.
/// </para>
/// </remarks>
public sealed class BalanceamentoTeste(ITestOutputHelper saida)
{
    private const int AmostrasDeLuta = 4000;
    private const int AmostrasDeCarreira = 200;

    [Fact]
    public void ADistribuicaoDeMetodosDeVitoriaLembraOMmaReal()
    {
        var motor = new MotorDeLuta();
        var perfil = PerfilDeCombate.Montar("Teste", Cenario.Atributos(85));

        var metodos = new Dictionary<MetodoDeEncerramento, int>();
        var empates = 0;

        for (var semente = 0; semente < AmostrasDeLuta; semente++)
        {
            var desfecho = motor.Simular(perfil, perfil, 3, new Sorteio(semente));
            metodos[desfecho.Metodo] = metodos.GetValueOrDefault(desfecho.Metodo) + 1;
            empates += desfecho.Resultado == ResultadoDaLuta.Empate ? 1 : 0;
        }

        ImprimirDistribuicao("Lutas de 3 rounds entre perfis idênticos (85)", metodos, AmostrasDeLuta);
        saida.WriteLine($"empates ......... {empates * 100.0 / AmostrasDeLuta:F1}%");

        Percentual(metodos[MetodoDeEncerramento.Decisao], AmostrasDeLuta).Should().BeInRange(45, 62);
        Percentual(metodos[MetodoDeEncerramento.Nocaute], AmostrasDeLuta).Should().BeInRange(22, 36);
        Percentual(metodos[MetodoDeEncerramento.Finalizacao], AmostrasDeLuta).Should().BeInRange(10, 24);
        Percentual(empates, AmostrasDeLuta).Should().BeLessThan(5, "empate no MMA é raro");
    }

    [Theory]
    [InlineData(65, 0, 0)]
    [InlineData(85, 25, 70)]
    [InlineData(96, 90, 100)]
    public void AChanceDeSerCampeaoAcompanhaAQualidadeDoDraft(
        int notaDoBuild,
        int minimoEsperado,
        int maximoEsperado)
    {
        var carreiras = SimularCarreiras(notaDoBuild);
        var campeoes = Percentual(carreiras.Count(carreira => carreira.FoiCampeao), carreiras.Count);

        saida.WriteLine($"build {notaDoBuild}: campeão em {campeoes:F1}% das carreiras");

        campeoes.Should().BeInRange(minimoEsperado, maximoEsperado);
    }

    [Fact]
    public void OTituloDeMaiorDeTodosOsTemposContinuaSendoRaroMesmoNoBuildPerfeito()
    {
        var carreiras = SimularCarreiras(96);

        var distribuicao = carreiras
            .GroupBy(carreira => carreira.Legado)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        ImprimirDistribuicao("Legados de um build 96", distribuicao, carreiras.Count);

        var maioresDeTodos = Percentual(
            distribuicao.GetValueOrDefault(NivelDeLegado.MaiorDeTodosOsTempos),
            carreiras.Count);

        // Um draft perfeito precisa dominar, mas o degrau mais alto tem que
        // continuar dependendo de como a carreira se desenrolou.
        maioresDeTodos.Should().BeInRange(8, 40);
    }

    [Fact]
    public void UmBuildMedianoProduzCarreirasDeTodosOsTamanhos()
    {
        var carreiras = SimularCarreiras(85);

        var niveisAlcancados = carreiras.Select(carreira => carreira.Legado).Distinct().ToList();

        saida.WriteLine($"build 85 alcançou {niveisAlcancados.Count} níveis de legado distintos");

        // Se um build mediano sempre terminasse no mesmo rótulo, o jogo não
        // teria releitura: a mesma montagem tem que render histórias diferentes.
        niveisAlcancados.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void AsCarreirasTerminamEmIdadesPlausiveis()
    {
        var carreiras = SimularCarreiras(85);

        var idadeMedia = carreiras.Average(carreira => carreira.IdadeDeAposentadoria);
        saida.WriteLine($"idade média de aposentadoria: {idadeMedia:F1}");

        idadeMedia.Should().BeInRange(30, 39);
        carreiras.Should().OnlyContain(carreira => carreira.IdadeDeAposentadoria <= 41);
        carreiras.Should().OnlyContain(carreira =>
            carreira.IdadeDeAposentadoria > carreira.IdadeDeEstreia);
    }

    /// <summary>
    /// Lesão precisa ser um acidente, não uma rotina nem uma lenda.
    /// </summary>
    /// <remarks>
    /// Se quase toda carreira passasse ilesa, o grau de dificuldade deixaria de
    /// pesar na decisão; se quase toda luta machucasse, o jogador só aceitaria
    /// as tranquilas e o jogo viraria uma fila de vitórias fáceis. A faixa é
    /// larga porque o que importa é o patamar.
    /// </remarks>
    [Fact]
    public void ALesaoAconteceOBastanteParaPesarNaDecisaoESemDominarACarreira()
    {
        var carreiras = SimularCarreiras(85);

        var comAlgumaLesao = Percentual(
            carreiras.Count(carreira => carreira.Estado.LesoesSofridas > 0),
            carreiras.Count);

        var lesoesPorLuta = Percentual(
            carreiras.Sum(carreira => carreira.Estado.LesoesSofridas),
            carreiras.Sum(carreira => carreira.TotalDeLutas));

        saida.WriteLine($"carreiras com ao menos uma lesão: {comAlgumaLesao:F1}%");
        saida.WriteLine($"lesões por luta disputada ......: {lesoesPorLuta:F1}%");

        comAlgumaLesao.Should().BeInRange(25, 90);
        lesoesPorLuta.Should().BeInRange(2, 18);
    }

    private static List<Api.Domain.Carreira> SimularCarreiras(int notaDoBuild)
    {
        var motor = new MotorDeCarreira(new MotorDeLuta(), new GeradorDeAdversarios());

        return Enumerable.Range(1, AmostrasDeCarreira)
            .Select(semente => motor.Simular(
                Cenario.PartidaComDraftConcluido(Cenario.Atributos(notaDoBuild), seed: semente)))
            .ToList();
    }

    private static double Percentual(int parte, int total) => parte * 100.0 / total;

    private void ImprimirDistribuicao<TChave>(string titulo, Dictionary<TChave, int> contagem, int total)
        where TChave : notnull
    {
        saida.WriteLine($"=== {titulo} ({total} amostras) ===");

        foreach (var (chave, quantidade) in contagem.OrderByDescending(par => par.Value))
        {
            saida.WriteLine($"  {chave,-28} {quantidade,5}  {Percentual(quantidade, total),5:F1}%");
        }
    }
}
