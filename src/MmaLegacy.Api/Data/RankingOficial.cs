using MmaLegacy.Api.Domain;
using MmaLegacy.Api.Domain.Enums;

using static MmaLegacy.Api.Data.NotasPorRanking.Arquetipo;

namespace MmaLegacy.Api.Data;

/// <summary>
/// Os ranqueados das oito divisões masculinas, na ordem do ranking.
/// </summary>
/// <remarks>
/// É contra estes atletas que a carreira acontece, e é por esta escada que o
/// jogador sobe. A posição na lista <b>é</b> a posição no ranking: o primeiro
/// da lista é o campeão.
/// <para>
/// As notas não estão escritas aqui — vêm de <see cref="NotasPorRanking"/>, a
/// partir da posição e do arquétipo. O que este arquivo declara é o que se
/// sabe de fato: quem compete, onde e em que ordem.
/// </para>
/// <para>
/// Rankings mudam toda semana. Atualizar é reescrever as listas abaixo e rodar
/// o seed de novo — como o Id vem do slug, atletas que permanecem apenas mudam
/// de posição, sem duplicar.
/// </para>
/// </remarks>
public static class RankingOficial
{
    /// <summary>Um atleta na tabela de uma divisão.</summary>
    /// <param name="Nome">Nome de exibição.</param>
    /// <param name="Pais">País de origem.</param>
    /// <param name="Arquetipo">Como as notas se distribuem entre as habilidades.</param>
    private sealed record Ranqueado(string Nome, string Pais, NotasPorRanking.Arquetipo Arquetipo);

    public static IEnumerable<Lutador> Montar() =>
        Divisoes.SelectMany(divisao => MontarDivisao(divisao.Key, divisao.Value));

    private static IEnumerable<Lutador> MontarDivisao(
        CategoriaDePeso categoria,
        IReadOnlyList<Ranqueado> tabela) =>
        tabela.Select((atleta, indice) => new Lutador(
            atleta.Nome,
            atleta.Pais,
            NotasPorRanking.Derivar(atleta.Nome, indice, atleta.Arquetipo),
            ehLenda: false,
            categoria: categoria,
            posicaoNoRanking: indice));

    /// <summary>Índice 0 é o campeão; 1 a 15, os ranqueados.</summary>
    private static readonly Dictionary<CategoriaDePeso, IReadOnlyList<Ranqueado>> Divisoes = new()
    {
        [CategoriaDePeso.Mosca] =
        [
            new("Joshua Van", "Mianmar", Striker),
            new("Alexandre Pantoja", "Brasil", Grappler),
            new("Manel Kape", "Angola", Striker),
            new("Brandon Royval", "Estados Unidos", Grappler),
            new("Tatsuro Taira", "Japão", Grappler),
            new("Asu Almabayev", "Cazaquistão", Wrestler),
            new("Lone'er Kavanagh", "Reino Unido", Striker),
            new("Ramazan Temirov", "Ucrânia", Wrestler),
            new("Kyoji Horiguchi", "Japão", Striker),
            new("Amir Albazi", "Iraque", Grappler),
            new("Brandon Moreno", "México", Completo),
            new("Kevin Borjas", "Peru", Wrestler),
            new("Mitch Raposo", "Estados Unidos", Completo),
            new("Sumudaerji", "China", Striker),
            new("Alessandro Costa", "Brasil", Striker),
            new("Alex Perez", "Estados Unidos", Wrestler)
        ],

        [CategoriaDePeso.Galo] =
        [
            new("Petr Yan", "Rússia", Tecnico),
            new("Merab Dvalishvili", "Geórgia", Wrestler),
            new("Umar Nurmagomedov", "Rússia", Wrestler),
            new("Sean O'Malley", "Estados Unidos", Striker),
            new("Mario Bautista", "Estados Unidos", Completo),
            new("Cory Sandhagen", "Estados Unidos", Tecnico),
            new("Song Yadong", "China", Striker),
            new("David Martinez", "México", Striker),
            new("Raoni Barcelos", "Brasil", Grappler),
            new("Farid Basharat", "Afeganistão", Wrestler),
            new("Marcus McGhee", "Estados Unidos", Striker),
            new("Deiveson Figueiredo", "Brasil", Completo),
            new("Aiemann Zahabi", "Canadá", Tecnico),
            new("Charles Jourdain", "Canadá", Striker),
            new("Bryce Mitchell", "Estados Unidos", Grappler),
            new("Montel Jackson", "Estados Unidos", Striker)
        ],

        [CategoriaDePeso.Pena] =
        [
            new("Alexander Volkanovski", "Austrália", Completo),
            new("Movsar Evloev", "Rússia", Wrestler),
            new("Diego Lopes", "Brasil", Grappler),
            new("Lerone Murphy", "Reino Unido", Tecnico),
            new("Aljamain Sterling", "Estados Unidos", Wrestler),
            new("Arnold Allen", "Reino Unido", Striker),
            new("Jean Silva", "Brasil", Striker),
            new("Pat Sabatini", "Estados Unidos", Grappler),
            new("Youssef Zalal", "Marrocos", Completo),
            new("Nathaniel Wood", "Reino Unido", Striker),
            new("Kevin Vallejos", "Argentina", Striker),
            new("Melquizael Costa", "Brasil", Grappler),
            new("Steve Garcia", "Estados Unidos", Striker),
            new("Aaron Pico", "Estados Unidos", Wrestler),
            new("Jose Miguel Delgado", "Estados Unidos", Striker),
            new("Joanderson Brito", "Brasil", Completo)
        ],

        [CategoriaDePeso.Leve] =
        [
            new("Justin Gaethje", "Estados Unidos", Striker),
            new("Ilia Topuria", "Espanha", Completo),
            new("Arman Tsarukyan", "Armênia", Wrestler),
            new("Charles Oliveira", "Brasil", Grappler),
            new("Max Holloway", "Estados Unidos", Striker),
            new("Paddy Pimblett", "Reino Unido", Grappler),
            new("Mateusz Gamrot", "Polônia", Wrestler),
            new("Renato Moicano", "Brasil", Grappler),
            new("Benoit Saint Denis", "França", Wrestler),
            new("Quillan Salkilld", "Austrália", Striker),
            new("Mauricio Ruffy", "Brasil", Striker),
            new("Tom Nolan", "Austrália", Striker),
            new("Dan Hooker", "Nova Zelândia", Striker),
            new("Rafael Fiziev", "Azerbaijão", Striker),
            new("Tofiq Musayev", "Azerbaijão", Striker),
            new("Grant Dawson", "Estados Unidos", Wrestler)
        ],

        [CategoriaDePeso.MeioMedio] =
        [
            new("Islam Makhachev", "Rússia", Wrestler),
            new("Carlos Prates", "Brasil", Striker),
            new("Ian Machado Garry", "Irlanda", Tecnico),
            new("Michael Morales", "Equador", Striker),
            new("Jack Della Maddalena", "Austrália", Striker),
            new("Sean Brady", "Estados Unidos", Grappler),
            new("Gabriel Bonfim", "Brasil", Grappler),
            new("Belal Muhammad", "Estados Unidos", Wrestler),
            new("Leon Edwards", "Reino Unido", Tecnico),
            new("Joaquin Buckley", "Estados Unidos", Striker),
            new("Uros Medic", "Sérvia", Striker),
            new("Kamaru Usman", "Nigéria", Wrestler),
            new("Mike Malott", "Canadá", Grappler),
            new("Michael Page", "Reino Unido", Striker),
            new("Yaroslav Amosov", "Ucrânia", Wrestler),
            new("Kevin Holland", "Estados Unidos", Striker)
        ],

        [CategoriaDePeso.Medio] =
        [
            new("Sean Strickland", "Estados Unidos", Tecnico),
            new("Khamzat Chimaev", "Emirados Árabes", Wrestler),
            new("Dricus Du Plessis", "África do Sul", Completo),
            new("Nassourdine Imavov", "França", Striker),
            new("Joe Pyfer", "Estados Unidos", Striker),
            new("Brendan Allen", "Estados Unidos", Grappler),
            new("Caio Borralho", "Brasil", Completo),
            new("Anthony Hernandez", "Estados Unidos", Wrestler),
            new("Israel Adesanya", "Nigéria", Striker),
            new("Gregory Rodrigues", "Brasil", Striker),
            new("Christian Leroy Duncan", "Reino Unido", Striker),
            new("Ikram Aliskerov", "Rússia", Wrestler),
            new("Bo Nickal", "Estados Unidos", Wrestler),
            new("Abus Magomedov", "Alemanha", Striker),
            new("Nursulton Ruziboev", "Uzbequistão", Striker),
            new("Reinier de Ridder", "Países Baixos", Grappler)
        ],

        [CategoriaDePeso.MeioPesado] =
        [
            new("Carlos Ulberg", "Nova Zelândia", Striker),
            new("Alex Pereira", "Brasil", Striker),
            new("Magomed Ankalaev", "Rússia", Completo),
            new("Jiri Prochazka", "Tchéquia", Striker),
            new("Paulo Costa", "Brasil", Striker),
            new("Jamahal Hill", "Estados Unidos", Striker),
            new("Khalil Rountree Jr.", "Estados Unidos", Striker),
            new("Navajo Stirling", "Nova Zelândia", Completo),
            new("Dominick Reyes", "Estados Unidos", Striker),
            new("Azamat Murzakanov", "Rússia", Completo),
            new("Bogdan Guskov", "Uzbequistão", Striker),
            new("Robert Whittaker", "Austrália", Tecnico),
            new("Johnny Walker", "Brasil", Striker),
            new("Alonzo Menifield", "Estados Unidos", Striker),
            new("Muhammad Saidov", "Tajiquistão", Wrestler),
            new("Iwo Baraniewski", "Polônia", Striker)
        ],

        [CategoriaDePeso.Pesado] =
        [
            new("Tom Aspinall", "Reino Unido", Completo),
            new("Ciryl Gane", "França", Tecnico),
            new("Alexander Volkov", "Rússia", Striker),
            new("Sergei Pavlovich", "Rússia", Striker),
            new("Rizvan Kuniev", "Rússia", Wrestler),
            new("Josh Hokit", "Estados Unidos", Wrestler),
            new("Waldo Cortes Acosta", "República Dominicana", Striker),
            new("Valter Walker", "Brasil", Grappler),
            new("Serghei Spivac", "Moldávia", Wrestler),
            new("Curtis Blaydes", "Estados Unidos", Wrestler),
            new("Vitor Petrino", "Brasil", Wrestler),
            new("Brando Pericic", "Croácia", Striker),
            new("Mario Pinto", "Portugal", Striker),
            new("Mick Parkin", "Reino Unido", Completo),
            new("Ryan Spann", "Estados Unidos", Striker)
        ]
    };
}
