using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;

namespace MmaLegacy.Tests.Support;

/// <summary>
/// Um banco SQLite em memória para os testes de fluxo.
/// </summary>
/// <remarks>
/// Não é o PostgreSQL de produção, e não pretende ser: a intenção é validar o
/// <b>mapeamento</b> — se o agregado da partida vai ao banco e volta inteiro,
/// com rodadas, lutador montado e carreira no lugar. Isso é o que quebra ao
/// mexer nas configurações do EF, e é o tipo de defeito que teste de unidade
/// nenhum pega.
/// <para>
/// A conexão fica aberta pelo tempo de vida da instância de propósito: o SQLite
/// em memória descarta o banco quando a última conexão fecha, e cada contexto
/// criado aqui abre a sua.
/// </para>
/// </remarks>
public sealed class BancoDeTeste : IDisposable
{
    private readonly SqliteConnection _conexao;

    public BancoDeTeste()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        using var contexto = CriarContexto();
        contexto.Database.EnsureCreated();
    }

    /// <summary>
    /// Cria um contexto novo sobre o mesmo banco.
    /// </summary>
    /// <remarks>
    /// Ler de volta em um contexto <b>diferente</b> daquele que gravou é o que
    /// dá valor ao teste: com o mesmo contexto, o EF devolveria os objetos do
    /// rastreador de mudanças e o mapeamento nunca seria exercitado.
    /// </remarks>
    public ContextoDoJogo CriarContexto() =>
        new(new DbContextOptionsBuilder<ContextoDoJogo>()
            .UseSqlite(_conexao)
            .Options);

    public void Dispose() => _conexao.Dispose();
}
