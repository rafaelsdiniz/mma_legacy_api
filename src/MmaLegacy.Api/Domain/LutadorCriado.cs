using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// O lutador que saiu do draft: os oito atributos roubados dos atletas do
/// acervo, mais tudo que se deduz deles.
/// </summary>
/// <remarks>
/// Overall e estilo são calculados no construtor e nunca recebidos de fora.
/// Não existe forma de montar um lutador com overall que não corresponda aos
/// seus atributos — nem por erro de código, nem por manipulação de requisição.
/// </remarks>
public sealed class LutadorCriado
{
    public Atributos Atributos { get; private set; } = null!;

    /// <summary>Nota geral ponderada, entre 1 e 100, com uma casa decimal.</summary>
    public decimal Overall { get; private set; }

    /// <summary>Estilo predominante deduzido dos atributos.</summary>
    public EstiloDeLuta Estilo { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private LutadorCriado()
    {
    }

    internal LutadorCriado(Atributos atributos)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        Atributos = atributos;
        Overall = CalculadoraDeOverall.Calcular(atributos);
        Estilo = IdentificadorDeEstilo.Identificar(atributos);
    }

    /// <summary>A habilidade mais alta — o que o jogador acertou no draft.</summary>
    public Habilidade MaiorQualidade => Atributos.MaiorHabilidade();

    /// <summary>A habilidade mais baixa — o buraco que a carreira vai cobrar.</summary>
    public Habilidade PrincipalFraqueza => Atributos.MenorHabilidade();
}
