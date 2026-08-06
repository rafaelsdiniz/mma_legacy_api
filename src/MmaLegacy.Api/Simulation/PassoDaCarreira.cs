using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Simulation;

/// <summary>
/// O que uma decisão do jogador produziu: a luta que saiu dela, se saiu alguma,
/// e tudo que mudou na carreira em seguida.
/// </summary>
/// <remarks>
/// A carreira jogada precisa contar o que aconteceu <b>entre</b> uma tela e a
/// próxima. Sem isto o jogador aceitaria uma luta, veria o cartel mudar de
/// 3-0 para 4-0 e não saberia que a vitória o promoveu à Grande Organização —
/// o que é justamente a parte que importa.
/// </remarks>
/// <param name="Luta">A linha do cartel gerada, ou <c>null</c> se o jogador recusou.</param>
/// <param name="Desfecho">
/// O round a round da luta. Existe só nesta resposta: é a narrativa do momento,
/// não histórico — persistir cinco rounds de detalhe para cada uma das trinta
/// lutas de uma carreira custaria caro para algo que ninguém relê.
/// </param>
/// <param name="Eventos">O que mudou na carreira, na ordem em que aconteceu.</param>
public sealed record PassoDaCarreira(
    LutaDaCarreira? Luta,
    ResultadoDaLutaSimulada? Desfecho,
    IReadOnlyList<EventoDaCarreira> Eventos)
{
    public static PassoDaCarreira Vazio => new(null, null, []);
}
