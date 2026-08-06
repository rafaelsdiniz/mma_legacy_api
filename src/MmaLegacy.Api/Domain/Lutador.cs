using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MmaLegacy.Api.Domain.Enums;
using MmaLegacy.Api.Domain.Exceptions;

namespace MmaLegacy.Api.Domain;

/// <summary>
/// Um atleta do acervo — a referência real que aparece no draft e da qual o
/// jogador rouba uma habilidade. Não é o lutador montado pelo jogador; esse é o
/// <see cref="LutadorCriado"/>.
/// </summary>
/// <remarks>
/// As notas aqui são estimativas editoriais do jogo e vivem exclusivamente no
/// servidor. O front-end recebe as notas para exibir, mas nunca as envia de
/// volta: na hora de registrar a escolha, o valor gravado é sempre o que está
/// aqui.
/// </remarks>
public sealed class Lutador
{
    public Guid Id { get; private set; }

    /// <summary>Nome de exibição, com acentuação.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Versão normalizada do nome, usada para achar a imagem do atleta.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>País de origem, para exibir a bandeira no card.</summary>
    public string Pais { get; private set; } = string.Empty;

    public Atributos Atributos { get; private set; } = null!;

    /// <summary>
    /// Atleta histórico, fora de atividade.
    /// </summary>
    /// <remarks>
    /// Lendas aparecem no draft — o jogo é sobre roubar habilidades das maiores
    /// referências do MMA, e isso não tem data de validade. Mas elas nunca são
    /// sorteadas como adversárias da carreira: Anderson Silva pode ceder o
    /// striking dele ao seu lutador, e não disputar um cinturão em 2027.
    /// <para>
    /// É também o que sustenta o modo Lendas, em que só este grupo entra no
    /// sorteio.
    /// </para>
    /// </remarks>
    public bool EhLenda { get; private set; }

    /// <summary>Divisão em que o atleta compete. Nulo para lendas fora de atividade.</summary>
    public CategoriaDePeso? Categoria { get; private set; }

    /// <summary>
    /// Posição no ranking da divisão: <c>0</c> para o campeão, <c>1</c> a
    /// <c>15</c> para os ranqueados, nulo para quem está fora do ranking.
    /// </summary>
    /// <remarks>
    /// É o que permite a carreira acontecer contra adversários reais e o
    /// jogador subir por uma escada concreta em vez de degraus abstratos.
    /// Zero para o campeão porque a ordem numérica passa a ser a ordem do
    /// ranking — ordenar por este campo já devolve a tabela pronta.
    /// </remarks>
    public int? PosicaoNoRanking { get; private set; }

    /// <summary>
    /// O atleta pode ser sorteado nas oito rodadas do draft.
    /// </summary>
    /// <remarks>
    /// Estar no acervo e ser sorteável são coisas diferentes, e essa separação é
    /// o que mantém o draft interessante. O acervo precisa dos quinze ranqueados
    /// de cada divisão para a carreira ter uma escada real para subir; o draft
    /// precisa de nomes que o jogador <b>reconheça</b> — roubar o wrestling do
    /// Khabib é uma decisão, roubar o wrestling do décimo quarto colocado do
    /// peso-galo é um sorteio.
    /// <para>
    /// Quem manda nisto é <c>ElencoDoDraft</c>, uma lista escrita à mão. Tirar
    /// alguém do draft não o tira do jogo: ele continua no ranking e continua
    /// podendo aparecer como adversário da carreira.
    /// </para>
    /// </remarks>
    public bool SorteavelNoDraft { get; private set; }

    /// <summary>Campeão da divisão.</summary>
    public bool EhCampeao => PosicaoNoRanking == 0;

    /// <summary>Está no ranking e pode ser adversário da carreira.</summary>
    public bool EstaRanqueado => !EhLenda && Categoria is not null && PosicaoNoRanking is not null;

    /// <summary>Construtor sem parâmetros exigido pelo Entity Framework.</summary>
    private Lutador()
    {
    }

    public Lutador(
        string nome,
        string pais,
        Atributos atributos,
        bool ehLenda = false,
        CategoriaDePeso? categoria = null,
        int? posicaoNoRanking = null)
    {
        ArgumentNullException.ThrowIfNull(atributos);

        if (posicaoNoRanking is { } posicao)
        {
            DadoInvalidoException.ExigirIntervalo(posicao, 0, 15, nameof(posicaoNoRanking));
        }

        RegraDeNegocioException.Se(
            posicaoNoRanking is not null && categoria is null,
            $"{nome} tem posição no ranking mas nenhuma categoria — um ranking existe dentro de uma divisão.");

        Nome = DadoInvalidoException.ExigirTextoPreenchido(nome, nameof(nome));
        Pais = DadoInvalidoException.ExigirTextoPreenchido(pais, nameof(pais));
        Slug = GerarSlug(Nome);
        Id = GerarIdDeterministico(Slug);
        Atributos = atributos;
        EhLenda = ehLenda;
        Categoria = categoria;
        PosicaoNoRanking = posicaoNoRanking;
    }

    /// <summary>
    /// Cópia deste atleta com a divisão e a posição de outro.
    /// </summary>
    /// <remarks>
    /// Serve à fusão do acervo: quando um atleta tem notas escritas à mão e
    /// também aparece no ranking, ficam valendo as notas manuais — que são
    /// melhores — e a colocação vinda da lista de ranking.
    /// </remarks>
    internal Lutador ComRankingDe(Lutador ranqueado)
    {
        ArgumentNullException.ThrowIfNull(ranqueado);

        var fundido = new Lutador(
            Nome,
            Pais,
            Atributos,
            EhLenda,
            ranqueado.Categoria,
            ranqueado.PosicaoNoRanking);

        fundido.SorteavelNoDraft = SorteavelNoDraft || ranqueado.SorteavelNoDraft;

        return fundido;
    }

    /// <summary>
    /// Marca se o atleta entra no sorteio do draft. Chamado pelo seed depois de
    /// montar o acervo, a partir da lista escrita à mão.
    /// </summary>
    internal void DefinirSorteioNoDraft(bool sorteavel) => SorteavelNoDraft = sorteavel;

    /// <summary>
    /// Converte "Alex Poatan" em "alex-poatan": sem acento, sem maiúscula e sem
    /// espaço, para servir de nome de arquivo de imagem no front-end.
    /// </summary>
    public static string GerarSlug(string nome)
    {
        var semAcento = nome.Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        var construtor = new StringBuilder(semAcento.Length);
        foreach (var caractere in new string(semAcento).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(caractere))
            {
                construtor.Append(caractere);
            }
            else if (construtor.Length > 0 && construtor[^1] != '-')
            {
                construtor.Append('-');
            }
        }

        return construtor.ToString().Trim('-');
    }

    /// <summary>
    /// Deriva o Id do slug em vez de sortear um Guid novo.
    /// </summary>
    /// <remarks>
    /// Assim o mesmo atleta tem sempre o mesmo Id em qualquer banco, e rodar o
    /// seed duas vezes atualiza os registros em vez de duplicá-los. Sem isso,
    /// um draft diário salvo hoje apontaria para Ids inexistentes depois de uma
    /// recriação do banco.
    /// </remarks>
    public static Guid GerarIdDeterministico(string slug)
    {
        var digest = MD5.HashData(Encoding.UTF8.GetBytes(slug));
        return new Guid(digest);
    }
}
