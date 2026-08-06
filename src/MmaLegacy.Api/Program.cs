using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Infrastructure;
using MmaLegacy.Api.Services;
using MmaLegacy.Api.Simulation;

var construtor = WebApplication.CreateBuilder(args);

const string PoliticaDeCors = "front-end";

/// <summary>
/// Ambiente usado pelos testes de contrato HTTP, que sobem a API inteira com
/// um banco próprio no lugar do PostgreSQL.
/// </summary>
const string AmbienteDeTeste = "Testing";

construtor.Services.AddControllers().AddJsonOptions(opcoes =>
{
    // Enums viajam como texto ("Nocaute", não 1). O front-end fica legível e
    // não quebra se um valor for inserido no meio de um enum.
    opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// O ambiente Testing não registra provider nenhum: a fábrica dos testes injeta
// o seu (SQLite em memória). Sem esta condição os dois ficariam no mesmo
// contêiner e o EF recusa a inicialização — "only a single database provider
// can be registered". Remover os serviços do Npgsql depois não resolve, porque
// AddDbContext registra bem mais do que as opções.
if (!construtor.Environment.IsEnvironment(AmbienteDeTeste))
{
    construtor.Services.AddDbContext<ContextoDoJogo>(opcoes =>
        opcoes.UseNpgsql(construtor.Configuration.GetConnectionString("DefaultConnection")));
}

// O motor não guarda estado entre chamadas: toda a aleatoriedade vive no
// Sorteio, criado por partida. Por isso pode ser singleton.
construtor.Services.AddSingleton<MotorDeLuta>();
construtor.Services.AddSingleton<GeradorDeAdversarios>();
construtor.Services.AddSingleton<MotorDeCarreira>();

// Os serviços dependem do DbContext, que é scoped por requisição.
construtor.Services.AddScoped<ServicoDePartida>();
construtor.Services.AddScoped<ServicoDeDraft>();
construtor.Services.AddScoped<ServicoDeCarreira>();

construtor.Services.AddProblemDetails();
construtor.Services.AddExceptionHandler<ManipuladorGlobalDeExcecoes>();

construtor.Services.AddOpenApi();

construtor.Services.AddCors(opcoes => opcoes.AddPolicy(PoliticaDeCors, politica =>
    politica
        .WithOrigins(construtor.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var aplicacao = construtor.Build();

// Precisa vir antes de tudo: é ele que transforma exceção em ProblemDetails.
aplicacao.UseExceptionHandler();

if (!aplicacao.Environment.IsEnvironment(AmbienteDeTeste))
{
    await PrepararBancoAsync(aplicacao);
}

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.MapOpenApi();
    aplicacao.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/openapi/v1.json", "MMA Legacy API");
        opcoes.RoutePrefix = "swagger";
    });
}

aplicacao.UseCors(PoliticaDeCors);
aplicacao.MapControllers();

// Endpoint de saúde e de aquecimento.
//
// Em plataforma que escala a zero — Cloud Run, Azure F1 — a aplicação dorme
// quando ninguém joga, e acordar leva alguns segundos. O front chama isto assim
// que o jogador abre a ficha de inscrição, então a API já está de pé quando ele
// termina de digitar o nome e clica em "Iniciar draft".
aplicacao.MapGet("/api/saude", () => Results.Ok(new { situacao = "no ar" }));

await aplicacao.RunAsync();

/// <summary>
/// Aplica as migrations pendentes e garante o acervo de atletas.
/// </summary>
/// <remarks>
/// Roda também em produção, e isso é uma escolha consciente com um custo real:
/// o correto em sistema com dado de usuário é migrar em passo separado e
/// revisável do deploy, porque uma migration ruim derruba a aplicação inteira
/// no ar em vez de falhar num passo isolado do pipeline.
/// <para>
/// Aqui o risco é aceitável — não há dado de terceiros, o banco é pequeno e o
/// projeto é de uma pessoa só. Se um dia houver conta de jogador, mova esta
/// chamada para um passo do deploy antes de subir a nova revisão.
/// </para>
/// <para>
/// O seed é idempotente — os Ids dos atletas vêm do slug —, então rodar a cada
/// inicialização atualiza as notas em vez de duplicar o elenco. É assim que se
/// rebalanceia o acervo: edita <c>AcervoDeLutadores</c> e sobe de novo.
/// </para>
/// </remarks>
static async Task PrepararBancoAsync(WebApplication aplicacao)
{
    using var escopo = aplicacao.Services.CreateScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<ContextoDoJogo>();

    await contexto.Database.MigrateAsync();
    await AcervoDeLutadores.SemearAsync(contexto);
}

/// <summary>
/// Torna a classe gerada pelas instruções de nível superior visível para os
/// testes de integração, que precisam dela em <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
