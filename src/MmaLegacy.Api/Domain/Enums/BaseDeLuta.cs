namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Arte marcial de origem do lutador. É puramente descritiva no MVP: entra na
/// ficha e no card compartilhável, mas não altera atributos nem a simulação.
/// </summary>
public enum BaseDeLuta
{
    Boxe = 1,
    MuayThai = 2,
    Karate = 3,
    Kickboxing = 4,
    Wrestling = 5,
    JiuJitsu = 6,
    Judo = 7,
    Sambo = 8,
    Taekwondo = 9,
    Capoeira = 10
}
