namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Veredito final da carreira. A ordem crescente é significativa: a
/// <see cref="Rules.CalculadoraDeLegado"/> converte a pontuação em nível
/// percorrendo esta escala de baixo para cima.
/// </summary>
public enum NivelDeLegado
{
    PromessaQueNaoCorrespondeu = 1,
    LutadorRegional = 2,
    VeteranoRespeitado = 3,
    CompetidorDeElite = 4,
    DesafianteAoCinturao = 5,
    CampeaoMundial = 6,
    CampeaoDominante = 7,
    DuploCampeao = 8,
    LendaDoMma = 9,
    MaiorDeTodosOsTempos = 10
}
