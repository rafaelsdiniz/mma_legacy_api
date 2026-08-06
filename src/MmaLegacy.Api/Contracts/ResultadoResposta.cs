using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Contracts;

/// <summary>
/// O pacote final da partida: quem o jogador montou e no que isso deu.
/// </summary>
/// <remarks>
/// Existe para que a tela de resultado e o card compartilhável façam uma
/// requisição só. Sem ele o front-end precisaria juntar partida, lutador e
/// carreira no cliente, e a imagem gerada poderia sair com dados de estados
/// diferentes.
/// </remarks>
public sealed record ResultadoResposta(
    Guid PartidaId,
    int Seed,
    FichaResposta Ficha,
    LutadorMontadoResposta Lutador,
    CarreiraResposta Carreira)
{
    public static ResultadoResposta DeDominio(Partida partida)
    {
        var lutador = partida.ExigirLutadorMontado();
        var carreira = partida.ExigirCarreiraSimulada();

        return new ResultadoResposta(
            partida.Id,
            partida.Seed,
            FichaResposta.DeDominio(partida.Ficha),
            LutadorMontadoResposta.DeDominio(partida.Ficha, lutador),
            CarreiraResposta.DeDominio(carreira));
    }
}
