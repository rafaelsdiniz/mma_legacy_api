using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Rules;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// A visão que o motor de luta tem de um atleta: quem é, do que é capaz e como
/// luta. Serve tanto para o lutador do jogador quanto para os adversários
/// gerados, que não existem como entidade persistida.
/// </summary>
/// <param name="Nome">Nome exibido no cartel.</param>
/// <param name="Atributos">Atributos no momento desta luta.</param>
/// <param name="Estilo">Estilo predominante, usado nos matchups.</param>
/// <param name="Overall">Nota geral, usada para calibrar e para o histórico.</param>
public sealed record PerfilDeCombate(
    string Nome,
    Atributos Atributos,
    EstiloDeLuta Estilo,
    decimal Overall)
{
    /// <summary>
    /// Monta o perfil derivando estilo e overall dos atributos atuais.
    /// </summary>
    /// <remarks>
    /// É recalculado a cada luta de propósito: como os atributos evoluem e
    /// decaem com a idade, um lutador pode estrear como nocauteador e terminar
    /// a carreira como contra-golpeador técnico, quando a velocidade cai e o
    /// fight IQ sobe.
    /// </remarks>
    public static PerfilDeCombate Montar(string nome, Atributos atributos) => new(
        nome,
        atributos,
        IdentificadorDeEstilo.Identificar(atributos),
        CalculadoraDeOverall.Calcular(atributos));
}
