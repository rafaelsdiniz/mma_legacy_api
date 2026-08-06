namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Categorias de peso disponíveis. A ordem numérica é crescente por peso, o que
/// permite descobrir a categoria seguinte em uma mudança de divisão.
/// </summary>
public enum CategoriaDePeso
{
    Mosca = 1,
    Galo = 2,
    Pena = 3,
    Leve = 4,
    MeioMedio = 5,
    Medio = 6,
    MeioPesado = 7,
    Pesado = 8
}
