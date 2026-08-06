using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain.Rules;

/// <summary>
/// Deduz o estilo predominante do lutador a partir dos atributos que o jogador
/// montou no draft. O estilo é o que dá ao motor de luta as vantagens e
/// desvantagens de matchup.
/// </summary>
/// <remarks>
/// Cada estilo é ancorado em um atributo primário <b>diferente</b> — potência,
/// wrestling, jiu-jítsu, fight IQ e velocidade. Sem essa separação os estilos
/// colapsariam entre si e quase todo lutador com striking alto cairia no mesmo
/// rótulo. O atributo secundário apenas desempata e dá nuance.
/// </remarks>
public static class IdentificadorDeEstilo
{
    /// <summary>
    /// Diferença máxima entre a maior e a menor nota para o lutador ser
    /// considerado completo. Acima disso ele tem um ponto forte claro o
    /// bastante para definir um estilo.
    /// </summary>
    public const int AmplitudeMaximaParaCompleto = 8;

    private const double PesoDoAtributoPrimario = 0.55;
    private const double PesoDoAtributoSecundario = 0.45;

    /// <summary>
    /// Estilos avaliados, do mais específico para o mais genérico. A ordem
    /// importa: em caso de empate exato de afinidade, vence o primeiro da lista,
    /// o que mantém a identificação determinística para uma mesma seed.
    /// </summary>
    private static readonly (EstiloDeLuta Estilo, Habilidade Primario, Habilidade Secundario)[] Perfis =
    [
        (EstiloDeLuta.Nocauteador, Habilidade.Potencia, Habilidade.Striking),
        (EstiloDeLuta.GrapplerCompleto, Habilidade.JiuJitsu, Habilidade.Wrestling),
        (EstiloDeLuta.WrestlerDePressao, Habilidade.Wrestling, Habilidade.Cardio),
        (EstiloDeLuta.ContraGolpeadorTecnico, Habilidade.InteligenciaDeLuta, Habilidade.Striking),
        (EstiloDeLuta.LutadorDeMovimentacao, Habilidade.Velocidade, Habilidade.Striking)
    ];

    public static EstiloDeLuta Identificar(Atributos atributos)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        if (atributos.Amplitude() <= AmplitudeMaximaParaCompleto)
        {
            return EstiloDeLuta.LutadorCompleto;
        }

        return Perfis
            .Select(perfil => (perfil.Estilo, Afinidade: CalcularAfinidade(atributos, perfil.Primario, perfil.Secundario)))
            .MaxBy(candidato => candidato.Afinidade)
            .Estilo;
    }

    private static double CalcularAfinidade(Atributos atributos, Habilidade primario, Habilidade secundario) =>
        (atributos[primario] * PesoDoAtributoPrimario) + (atributos[secundario] * PesoDoAtributoSecundario);
}
