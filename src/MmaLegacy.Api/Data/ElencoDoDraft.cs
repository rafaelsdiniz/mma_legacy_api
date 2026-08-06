using MmaLegacy.Api.Domain;

namespace MmaLegacy.Api.Data;

/// <summary>
/// Quem pode ser sorteado nas oito rodadas do draft.
/// </summary>
/// <remarks>
/// O acervo tem cento e cinquenta atletas porque a carreira precisa de uma
/// escada real para subir: os quinze ranqueados de cada uma das oito divisões.
/// O draft não precisa disso — precisa do contrário. Sortear o décimo quarto
/// colocado do peso-galo transforma a decisão do jogador em chute, porque ele
/// não faz ideia de quem é aquele nome nem do que esperar das notas.
/// <para>
/// Por isso esta lista existe separada, escrita à mão: o draft é sobre roubar
/// uma habilidade de alguém que você <b>reconhece</b>. Estar fora dela não tira
/// ninguém do jogo — quem está no ranking continua no ranking e continua
/// podendo aparecer como adversário da carreira.
/// </para>
/// <para>
/// Os nomes precisam bater exatamente com os do acervo. O teste
/// <c>ElencoDoDraftTeste</c> garante isso: um nome escrito errado aqui falharia
/// em silêncio, deixando o atleta fora do draft sem ninguém perceber.
/// </para>
/// </remarks>
public static class ElencoDoDraft
{
    /// <summary>Os atletas sorteáveis, agrupados como o jogador os conhece.</summary>
    private static readonly string[] Nomes =
    [
        // Peso-pesado
        "Jon Jones",
        "Francis Ngannou",
        "Tom Aspinall",
        "Ciryl Gane",
        "Curtis Blaydes",
        "Alexander Volkov",
        "Sergei Pavlovich",

        // Meio-pesado
        "Daniel Cormier",
        "Alex Pereira",
        "Jiri Prochazka",
        "Glover Teixeira",
        "Maurício Rua",
        "Magomed Ankalaev",
        "Carlos Ulberg",
        "Jamahal Hill",
        "Johnny Walker",
        "Khalil Rountree Jr.",

        // Médio
        "Anderson Silva",
        "Israel Adesanya",
        "Sean Strickland",
        "Khamzat Chimaev",
        "Dricus Du Plessis",
        "Paulo Costa",
        "Vitor Belfort",
        "Caio Borralho",
        "Nassourdine Imavov",
        "Bo Nickal",
        "Reinier de Ridder",

        // Meio-médio
        "Georges St-Pierre",
        "Kamaru Usman",
        "Leon Edwards",
        "Belal Muhammad",
        "Jack Della Maddalena",
        "Ian Machado Garry",
        "Carlos Prates",
        "Sean Brady",
        "Joaquin Buckley",
        "Michael Morales",
        "Michael Page",
        "Kevin Holland",

        // Leve
        "Khabib Nurmagomedov",
        "Conor McGregor",
        "Islam Makhachev",
        "Charles Oliveira",
        "Justin Gaethje",
        "Dustin Poirier",
        "Arman Tsarukyan",
        "Dan Hooker",
        "Paddy Pimblett",
        "Mauricio Ruffy",
        "Renato Moicano",
        "Mateusz Gamrot",
        "Rafael Fiziev",

        // Pena
        "José Aldo",
        "Max Holloway",
        "Alexander Volkanovski",
        "Ilia Topuria",
        "Movsar Evloev",
        "Diego Lopes",
        "Jean Silva",
        "Bryce Mitchell",
        "Zabit Magomedsharipov",
        "Lerone Murphy",
        "Arnold Allen",

        // Galo
        "Petr Yan",
        "Aljamain Sterling",
        "Sean O'Malley",
        "Merab Dvalishvili",
        "Umar Nurmagomedov",
        "Cory Sandhagen",
        "Song Yadong",

        // Mosca
        "Demetrious Johnson",
        "Henry Cejudo",
        "Deiveson Figueiredo",
        "Brandon Moreno",
        "Alexandre Pantoja",
        "Manel Kape",
        "Kyoji Horiguchi",
        "Amir Albazi",

        // Fora das divisões atuais, mas grandes demais para ficar de fora
        "Fedor Emelianenko",
        "Antônio Rodrigo Nogueira",
        "Amanda Nunes"
    ];

    /// <summary>
    /// Os slugs correspondentes. O slug é a chave real porque é dele que sai o
    /// Id do atleta — comparar por nome escrito daria diferença por acento.
    /// </summary>
    private static readonly HashSet<string> Slugs =
        Nomes.Select(Lutador.GerarSlug).ToHashSet();

    /// <summary>Nomes como foram escritos, para o teste conferir um por um.</summary>
    public static IReadOnlyList<string> NomesEscritos => Nomes;

    public static bool Contem(Lutador lutador)
    {
        ArgumentNullException.ThrowIfNull(lutador);

        return Slugs.Contains(lutador.Slug);
    }

    /// <summary>Quantos atletas a lista pretende habilitar.</summary>
    public static int Quantidade => Slugs.Count;
}
