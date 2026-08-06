using MmaLegacy.Api.Domain.Enums;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Uma linha do cartel: quem foi o adversário, em que contexto a luta
/// aconteceu e como terminou.
/// </summary>
/// <remarks>
/// É o registro bruto da carreira. Todos os números do resumo final — cartel,
/// métodos de vitória, defesas de cinturão, maior sequência — são derivados
/// desta lista por <see cref="Carreira.Encerrar"/>, nunca somados à mão pelo
/// motor. Uma fonte da verdade só.
/// </remarks>
public sealed class LutaDaCarreira
{
    /// <summary>Posição da luta no cartel, começando em 1.</summary>
    public int Ordem { get; private set; }

    /// <summary>Idade do lutador na data da luta.</summary>
    public int Idade { get; private set; }

    public string Adversario { get; private set; } = string.Empty;

    /// <summary>Overall do adversário — a medida de qualidade do cartel.</summary>
    public decimal OverallDoAdversario { get; private set; }

    public EstiloDeLuta EstiloDoAdversario { get; private set; }

    public NivelDaOrganizacao Organizacao { get; private set; }

    /// <summary>Categoria em que a luta foi disputada, que muda se o lutador subir de divisão.</summary>
    public CategoriaDePeso Categoria { get; private set; }

    /// <summary>Luta em que o lutador desafia por um cinturão que ainda não tem.</summary>
    public bool DisputaDeCinturao { get; private set; }

    /// <summary>Luta em que o lutador põe em jogo o cinturão que já é dele.</summary>
    public bool DefesaDeCinturao { get; private set; }

    public int RoundsProgramados { get; private set; }

    public ResultadoDaLuta Resultado { get; private set; }

    public MetodoDeEncerramento Metodo { get; private set; }

    /// <summary>Round em que a luta acabou; igual a <see cref="RoundsProgramados"/> nas decisões.</summary>
    public int RoundDoEncerramento { get; private set; }

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private LutaDaCarreira()
    {
    }

    internal LutaDaCarreira(
        int ordem,
        int idade,
        string adversario,
        decimal overallDoAdversario,
        EstiloDeLuta estiloDoAdversario,
        NivelDaOrganizacao organizacao,
        CategoriaDePeso categoria,
        bool disputaDeCinturao,
        bool defesaDeCinturao,
        int roundsProgramados,
        ResultadoDaLuta resultado,
        MetodoDeEncerramento metodo,
        int roundDoEncerramento)
    {
        Ordem = ordem;
        Idade = idade;
        Adversario = adversario;
        OverallDoAdversario = overallDoAdversario;
        EstiloDoAdversario = estiloDoAdversario;
        Organizacao = organizacao;
        Categoria = categoria;
        DisputaDeCinturao = disputaDeCinturao;
        DefesaDeCinturao = defesaDeCinturao;
        RoundsProgramados = roundsProgramados;
        Resultado = resultado;
        Metodo = metodo;
        RoundDoEncerramento = roundDoEncerramento;
    }

    /// <summary>Luta de campeonato, seja conquistando ou defendendo o cinturão.</summary>
    public bool ValendoCinturao => DisputaDeCinturao || DefesaDeCinturao;
}
