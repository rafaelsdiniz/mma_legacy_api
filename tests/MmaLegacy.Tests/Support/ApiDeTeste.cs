using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MmaLegacy.Api.Data;

namespace MmaLegacy.Tests.Support;

/// <summary>
/// Sobe a API inteira em memória, trocando apenas o PostgreSQL por SQLite.
/// </summary>
/// <remarks>
/// Existe por causa de um bug real: os contratos usavam <c>[property: Required]</c>
/// em records posicionais, o que faz o MVC recusar a requisição com
/// <c>InvalidOperationException</c>. Nenhum teste pegou, porque todos chamavam
/// os serviços diretamente. Model binding, validação, roteamento e o
/// tratamento global de exceções só são exercitados atravessando o HTTP —
/// então é isso que esta fábrica permite fazer.
/// </remarks>
public sealed class ApiDeTeste : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder construtor)
    {
        // Testing evita que o Program tente migrar e semear o banco sozinho:
        // aqui o esquema é criado por EnsureCreated e o acervo entra na mão.
        construtor.UseEnvironment("Testing");

        construtor.ConfigureServices(servicos =>
        {
            _conexao.Open();
            servicos.AddDbContext<ContextoDoJogo>(opcoes => opcoes.UseSqlite(_conexao));
        });
    }

    /// <summary>Cria o esquema e semeia o acervo. Chame antes do primeiro request.</summary>
    public async Task PrepararAsync()
    {
        using var escopo = Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<ContextoDoJogo>();

        await contexto.Database.EnsureCreatedAsync();
        await AcervoDeLutadores.SemearAsync(contexto);
    }

    protected override void Dispose(bool descartando)
    {
        base.Dispose(descartando);

        if (descartando)
        {
            _conexao.Dispose();
        }
    }
}
