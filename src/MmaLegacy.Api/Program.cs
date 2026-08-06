using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MmaLegacy.Api.Data;
using MmaLegacy.Api.Infrastructure;
using MmaLegacy.Api.Services;
using MmaLegacy.Api.Simulation;

var construtor = WebApplication.CreateBuilder(args);

const string PoliticaDeCors = "front-end";

construtor.Services.AddControllers().AddJsonOptions(opcoes =>
{
    // Enums viajam como texto ("Nocaute", não 1). O front-end fica legível e
    // não quebra se um valor for inserido no meio de um enum.
    opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

construtor.Services.AddDbContext<ContextoDoJogo>(opcoes =>
    opcoes.UseNpgsql(construtor.Configuration.GetConnectionString("DefaultConnection")));

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

if (aplicacao.Environment.IsDevelopment())
{
    await PrepararBancoDeDesenvolvimentoAsync(aplicacao);

    aplicacao.MapOpenApi();
    aplicacao.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/openapi/v1.json", "MMA Legacy API");
        opcoes.RoutePrefix = "swagger";
    });
}

aplicacao.UseCors(PoliticaDeCors);
aplicacao.MapControllers();

await aplicacao.RunAsync();

/// <summary>
/// Aplica as migrations pendentes e garante o acervo de atletas.
/// </summary>
/// <remarks>
/// Só roda em desenvolvimento. Migrar automaticamente é conveniente na máquina
/// de quem desenvolve e perigoso em produção, onde a migration deve ser um
/// passo explícito e revisável do deploy.
/// <para>
/// O seed é idempotente — os Ids dos atletas vêm do slug —, então rodar a cada
/// inicialização atualiza as notas em vez de duplicar o elenco. É assim que se
/// rebalanceia o acervo: edita <c>AcervoDeLutadores</c> e reinicia a API.
/// </para>
/// </remarks>
static async Task PrepararBancoDeDesenvolvimentoAsync(WebApplication aplicacao)
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
