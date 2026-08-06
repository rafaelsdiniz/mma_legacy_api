using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data;

/// <summary>
/// O acervo de atletas que alimenta o draft.
/// </summary>
/// <remarks>
/// As notas são estimativas editoriais do jogo, feitas para equilibrar o draft
/// — não são avaliações oficiais nem medem o atleta real. Cada linha é o auge
/// da carreira do lutador, não o momento atual: é isso que permite Anderson
/// Silva e Alex Pereira aparecerem no mesmo sorteio.
/// <para>
/// A regra de balanceamento é que ninguém seja nota alta em tudo. Todo atleta
/// tem pelo menos um buraco, senão a decisão do draft — pegar a especialidade
/// ou o atributo que ainda falta — deixaria de existir.
/// </para>
/// </remarks>
public static class AcervoDeLutadores
{
    /// <summary>
    /// Insere ou atualiza o acervo no banco.
    /// </summary>
    /// <remarks>
    /// É idempotente: como o Id de cada atleta é derivado do slug, rodar o seed
    /// duas vezes atualiza as notas em vez de duplicar o elenco. Isso torna
    /// seguro chamar isto a cada inicialização da API em desenvolvimento e
    /// permite rebalancear notas apenas editando este arquivo.
    /// </remarks>
    public static async Task SemearAsync(ContextoDoJogo contexto, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var noBanco = await contexto.Lutadores.ToDictionaryAsync(
            lutador => lutador.Id,
            cancelamento);

        foreach (var lutador in Montar())
        {
            if (!noBanco.ContainsKey(lutador.Id))
            {
                contexto.Lutadores.Add(lutador);
            }
            else
            {
                contexto.Entry(noBanco[lutador.Id]).CurrentValues.SetValues(lutador);
                contexto.Entry(noBanco[lutador.Id]).Reference(atleta => atleta.Atributos).TargetEntry!
                    .CurrentValues.SetValues(lutador.Atributos);
            }
        }

        await contexto.SaveChangesAsync(cancelamento);
    }

    /// <summary>Constrói o acervo em memória, sem tocar no banco.</summary>
    public static IReadOnlyList<Lutador> Montar() =>
    [
        //       nome                     país           str pot vel wre jiu car res iq
        Lenda("Anderson Silva", "Brasil", 97, 92, 90, 74, 88, 84, 86, 96),
        Criar("Alex Pereira", "Brasil", 96, 99, 87, 75, 76, 87, 92, 93),
        Lenda("José Aldo", "Brasil", 94, 91, 95, 84, 82, 82, 88, 93),
        Criar("Charles Oliveira", "Brasil", 88, 89, 84, 78, 99, 88, 84, 87),
        Lenda("Amanda Nunes", "Brasil", 93, 95, 88, 87, 88, 84, 87, 91),
        Lenda("Maurício Rua", "Brasil", 89, 91, 83, 79, 84, 78, 82, 82),
        Lenda("Antônio Rodrigo Nogueira", "Brasil", 78, 80, 70, 76, 96, 84, 95, 89),
        Lenda("Vitor Belfort", "Brasil", 88, 95, 93, 70, 82, 70, 78, 76),
        Criar("Deiveson Figueiredo", "Brasil", 87, 92, 85, 82, 88, 78, 84, 83),
        Lenda("Glover Teixeira", "Brasil", 82, 86, 74, 86, 90, 80, 86, 85),

        Lenda("Khabib Nurmagomedov", "Rússia", 78, 80, 84, 99, 90, 94, 92, 95),
        Criar("Islam Makhachev", "Rússia", 84, 82, 85, 96, 92, 93, 90, 94),
        Lenda("Fedor Emelianenko", "Rússia", 88, 94, 86, 88, 90, 82, 90, 90),
        Criar("Khamzat Chimaev", "Emirados Árabes", 85, 88, 88, 95, 88, 82, 86, 84),
        Criar("Petr Yan", "Rússia", 92, 84, 88, 88, 78, 92, 88, 92),

        Criar("Jon Jones", "Estados Unidos", 93, 90, 88, 95, 88, 88, 96, 98),
        Lenda("Daniel Cormier", "Estados Unidos", 84, 88, 80, 96, 84, 86, 92, 93),
        Criar("Kamaru Usman", "Estados Unidos", 88, 87, 82, 96, 80, 92, 90, 92),
        Lenda("Jordan Burroughs", "Estados Unidos", 60, 70, 88, 99, 72, 90, 84, 88),
        Lenda("Henry Cejudo", "Estados Unidos", 86, 82, 90, 97, 76, 90, 84, 92),
        Lenda("Randy Couture", "Estados Unidos", 76, 78, 70, 94, 78, 84, 90, 94),
        Lenda("Cain Velasquez", "Estados Unidos", 86, 90, 84, 95, 80, 96, 86, 88),
        Criar("Jonathan Dwight Griffin", "Estados Unidos", 80, 76, 78, 78, 76, 94, 92, 80),
        Criar("Sean O'Malley", "Estados Unidos", 92, 88, 94, 68, 74, 82, 74, 86),
        Criar("Dustin Poirier", "Estados Unidos", 92, 90, 84, 80, 86, 86, 88, 86),
        Criar("Merab Dvalishvili", "Geórgia", 78, 72, 86, 96, 78, 99, 88, 88),

        Criar("Max Holloway", "Estados Unidos", 95, 82, 90, 72, 80, 98, 94, 92),
        Lenda("Georges St-Pierre", "Canadá", 90, 82, 90, 96, 88, 94, 90, 99),
        Lenda("Demetrious Johnson", "Estados Unidos", 88, 76, 96, 92, 90, 95, 82, 97),
        Lenda("Conor McGregor", "Irlanda", 92, 97, 92, 66, 72, 72, 78, 88),
        Lenda("Michael Bisping", "Reino Unido", 86, 74, 82, 80, 76, 92, 90, 88),
        Criar("Leon Edwards", "Reino Unido", 89, 84, 86, 82, 82, 90, 86, 89),
        Criar("Israel Adesanya", "Nigéria", 96, 90, 92, 64, 70, 86, 84, 94),
        Lenda("Francis Ngannou", "Camarões", 82, 100, 84, 74, 68, 70, 84, 78),
        Criar("Kamal Ibrahimov", "Azerbaijão", 84, 86, 80, 88, 90, 84, 82, 84),
        Criar("Alexander Volkanovski", "Austrália", 92, 86, 90, 90, 80, 96, 90, 96),
        Criar("Robert Whittaker", "Austrália", 91, 86, 90, 86, 78, 90, 88, 90),
        Criar("Ilia Topuria", "Espanha", 92, 94, 88, 88, 90, 84, 84, 88),
        Criar("Jiri Prochazka", "Tchéquia", 88, 94, 86, 74, 84, 88, 88, 74),
        Lenda("Zabit Magomedsharipov", "Rússia", 90, 82, 90, 86, 92, 84, 82, 86)
    ];

    /// <summary>Atleta em atividade. Entra no draft e pode ser adversário na carreira.</summary>
    private static Lutador Criar(
        string nome,
        string pais,
        int striking,
        int potencia,
        int velocidade,
        int wrestling,
        int jiuJitsu,
        int cardio,
        int resistencia,
        int inteligenciaDeLuta) =>
        Definir(nome, pais, striking, potencia, velocidade, wrestling, jiuJitsu, cardio,
            resistencia, inteligenciaDeLuta, ehLenda: false);

    /// <summary>
    /// Atleta histórico. Entra no draft, mas nunca é sorteado como adversário
    /// da carreira — o cartel simulado se passa no presente.
    /// </summary>
    private static Lutador Lenda(
        string nome,
        string pais,
        int striking,
        int potencia,
        int velocidade,
        int wrestling,
        int jiuJitsu,
        int cardio,
        int resistencia,
        int inteligenciaDeLuta) =>
        Definir(nome, pais, striking, potencia, velocidade, wrestling, jiuJitsu, cardio,
            resistencia, inteligenciaDeLuta, ehLenda: true);

    private static Lutador Definir(
        string nome,
        string pais,
        int striking,
        int potencia,
        int velocidade,
        int wrestling,
        int jiuJitsu,
        int cardio,
        int resistencia,
        int inteligenciaDeLuta,
        bool ehLenda) =>
        new(nome, pais, new Atributos(
            striking,
            potencia,
            velocidade,
            wrestling,
            jiuJitsu,
            cardio,
            resistencia,
            inteligenciaDeLuta), ehLenda);
}
