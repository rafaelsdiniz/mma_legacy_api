using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MmaLegacy.Tests.Support;

namespace MmaLegacy.Tests.Integration;

/// <summary>
/// Atravessa o HTTP de verdade: roteamento, model binding, validação e o
/// tratamento global de exceções.
/// </summary>
/// <remarks>
/// Os testes de <see cref="FluxoDaPartidaTeste"/> chamam os serviços direto e
/// por isso não enxergam nada desta camada. Foi essa cegueira que deixou passar
/// o <c>[property: Required]</c> em record posicional, que fazia toda requisição
/// de criação de partida virar 500.
/// </remarks>
public sealed class ContratoHttpTeste : IAsyncLifetime
{
    private static readonly JsonSerializerOptions OpcoesDeLeitura = new(JsonSerializerDefaults.Web);

    private readonly ApiDeTeste _api = new();
    private HttpClient _cliente = null!;

    public async Task InitializeAsync()
    {
        _cliente = _api.CreateClient();
        await _api.PrepararAsync();
    }

    [Fact]
    public async Task CriarPartidaComFichaValidaDevolve201()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/partidas", FichaValida());

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);

        var corpo = await LerJsonAsync(resposta);
        corpo.GetProperty("status").GetString().Should().Be("DraftEmAndamento");
        corpo.GetProperty("totalDeRodadas").GetInt32().Should().Be(8);
        corpo.GetProperty("ficha").GetProperty("nomeDeCartaz").GetString()
            .Should().Be("Rafael \"The Machine\" Diniz");
    }

    [Fact]
    public async Task OsEnumsViajamComoTextoENaoComoNumero()
    {
        var criacao = await _cliente.PostAsJsonAsync("/api/partidas", FichaValida());
        var corpo = await LerJsonAsync(criacao);

        // O front-end recebe "MeioPesado", não 7. Se isto quebrar, toda a
        // tipagem do cliente TypeScript quebra junto.
        corpo.GetProperty("ficha").GetProperty("categoriaDePeso").ValueKind
            .Should().Be(JsonValueKind.String);
        corpo.GetProperty("ficha").GetProperty("baseDeLuta").GetString().Should().Be("MuayThai");
    }

    [Fact]
    public async Task FichaInvalidaDevolve400ComErroPorCampoEmPortugues()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/partidas", new
        {
            nome = "A",
            apelido = "",
            nacionalidade = "Brasil",
            idadeInicial = 12,
            baseDeLuta = "Boxe"
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var erros = (await LerJsonAsync(resposta)).GetProperty("errors");
        erros.TryGetProperty("CategoriaDePeso", out var categoria).Should().BeTrue();
        categoria[0].GetString().Should().Be("Escolha a categoria de peso.");
        erros.GetProperty("IdadeInicial")[0].GetString()
            .Should().Be("A idade de estreia deve estar entre 18 e 35 anos.");
    }

    [Fact]
    public async Task PartidaInexistenteDevolve404ComProblemDetails()
    {
        var resposta = await _cliente.GetAsync($"/api/partidas/{Guid.NewGuid()}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await LerJsonAsync(resposta);
        corpo.GetProperty("title").GetString().Should().Be("Recurso não encontrado");
        corpo.GetProperty("detail").GetString().Should().Contain("não foi encontrado");
    }

    [Fact]
    public async Task HabilidadeJaOcupadaDevolve409ComMensagemParaOJogador()
    {
        var partidaId = await CriarPartidaAsync();

        var primeira = await ObterRodadaAtualAsync(partidaId);
        await EscolherAsync(partidaId, IdDoAtleta(primeira), "Striking");

        var segunda = await ObterRodadaAtualAsync(partidaId);
        var repetida = await EscolherAsync(partidaId, IdDoAtleta(segunda), "Striking");

        repetida.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await LerJsonAsync(repetida);
        corpo.GetProperty("title").GetString().Should().Be("Jogada inválida");
        corpo.GetProperty("detail").GetString().Should().Contain("Striking já foi preenchida");
    }

    [Fact]
    public async Task SimularCarreiraAntesDeTerminarODraftDevolve409()
    {
        var partidaId = await CriarPartidaAsync();

        var resposta = await _cliente.PostAsync($"/api/partidas/{partidaId}/carreira/simular", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OFluxoCompletoTerminaComOResultadoMontado()
    {
        var partidaId = await CriarPartidaAsync();

        for (var rodada = 0; rodada < 8; rodada++)
        {
            var atual = await ObterRodadaAtualAsync(partidaId);
            var habilidade = atual.GetProperty("habilidadesDisponiveis")[0].GetString()!;

            (await EscolherAsync(partidaId, IdDoAtleta(atual), habilidade))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var simulacao = await _cliente.PostAsync($"/api/partidas/{partidaId}/carreira/simular", null);
        simulacao.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultado = await _cliente.GetAsync($"/api/partidas/{partidaId}/resultado");
        resultado.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await LerJsonAsync(resultado);
        corpo.GetProperty("lutador").GetProperty("atributos").GetArrayLength().Should().Be(8);
        corpo.GetProperty("carreira").GetProperty("cartel").GetString().Should().MatchRegex(@"^\d+-\d+-\d+$");
        corpo.GetProperty("carreira").GetProperty("conquistas").GetArrayLength().Should().BeGreaterThan(0);
        corpo.GetProperty("carreira").GetProperty("lutas").GetArrayLength().Should().BeGreaterThan(0);
    }

    private static object FichaValida() => new
    {
        nome = "Rafael Diniz",
        apelido = "The Machine",
        nacionalidade = "Brasil",
        categoriaDePeso = "MeioPesado",
        idadeInicial = 22,
        baseDeLuta = "MuayThai",
        seed = 20260805
    };

    private async Task<Guid> CriarPartidaAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/partidas", FichaValida());
        resposta.EnsureSuccessStatusCode();

        return (await LerJsonAsync(resposta)).GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> ObterRodadaAtualAsync(Guid partidaId)
    {
        var resposta = await _cliente.GetAsync($"/api/partidas/{partidaId}/draft/atual");
        resposta.EnsureSuccessStatusCode();

        return await LerJsonAsync(resposta);
    }

    private Task<HttpResponseMessage> EscolherAsync(Guid partidaId, Guid atletaId, string habilidade) =>
        _cliente.PostAsJsonAsync(
            $"/api/partidas/{partidaId}/draft/escolher",
            new { atletaId, habilidade });

    private static Guid IdDoAtleta(JsonElement rodada) =>
        rodada.GetProperty("atleta").GetProperty("id").GetGuid();

    private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta) =>
        JsonSerializer.Deserialize<JsonElement>(await resposta.Content.ReadAsStringAsync(), OpcoesDeLeitura);

    public Task DisposeAsync()
    {
        _cliente.Dispose();
        _api.Dispose();

        return Task.CompletedTask;
    }
}
