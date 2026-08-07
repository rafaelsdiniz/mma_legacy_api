namespace MmaLegacy.Api.Domain.Enums;

/// <summary>
/// Com que peso o lutador treinou no camp que antecede a luta.
/// </summary>
/// <remarks>
/// É a segunda metade da decisão de aceitar uma oferta. Escolher a luta diz
/// contra quem se vai lutar; escolher a intensidade diz com que corpo. Treino
/// pesado evolui mais rápido e chega mais quebrado — e é justamente por isso
/// que ele não é a resposta certa para toda luta.
/// </remarks>
public enum IntensidadeDoTreino
{
    /// <summary>
    /// Camp de manutenção. Não evolui nada e reduz o risco de lesão: é o que se
    /// faz quando a luta já é dura o bastante.
    /// </summary>
    Leve = 1,

    /// <summary>O camp normal: evolui um pouco, arrisca o normal.</summary>
    Padrao = 2,

    /// <summary>
    /// Camp puxado. Quase dobra a chance de ganhar o ponto e cobra caro em
    /// risco — o corpo entra no octógono já castigado.
    /// </summary>
    Pesado = 3
}
