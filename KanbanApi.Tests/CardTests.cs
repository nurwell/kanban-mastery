using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KanbanApi.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KanbanApi.Tests;

public class CardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                             || d.ServiceType == typeof(ApplicationDbContext)
                             || (d.ServiceType.IsGenericType &&
                                 d.ServiceType.GetGenericTypeDefinition().FullName!
                                     .Contains("IDbContextOptionsConfiguration")))
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("CardTestDb"));
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password = "Test123!")
    {
        await _client.PostAsJsonAsync("/register", new { email, password });
        var loginResponse = await _client.PostAsJsonAsync("/login", new { email, password });
        var body = await loginResponse.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<int> CreateBoardAsync(string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { boardName = "Card Test Board" });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateColumnAsync(string token, int boardId, string title = "To Do")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/columns");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateCardAsync(string token, int boardId, int columnId, string title = "My Card")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/cards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title, description = "A test card", columnId });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    // --- Full lifecycle ---

    [Fact]
    public async Task CreateCard_ByMember_Returns201()
    {
        var token = await RegisterAndLoginAsync("card-create@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId, "Backlog");

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/cards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "Build feature X", description = "Details here", columnId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Build feature X", doc.GetProperty("title").GetString());
        Assert.Equal(columnId, doc.GetProperty("columnId").GetInt32());
    }

    [Fact]
    public async Task UpdateCard_Title_ByMember_Returns200()
    {
        var token = await RegisterAndLoginAsync("card-update@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId);
        var cardId = await CreateCardAsync(token, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "Renamed Card", description = "Updated desc", columnId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Renamed Card", doc.GetProperty("title").GetString());
    }

    [Fact]
    public async Task MoveCard_ToDifferentColumn_Returns200WithNewColumnId()
    {
        var token = await RegisterAndLoginAsync("card-move@example.com");
        var boardId = await CreateBoardAsync(token);
        var colToDo = await CreateColumnAsync(token, boardId, "To Do");
        var colDone = await CreateColumnAsync(token, boardId, "Done");
        var cardId = await CreateCardAsync(token, boardId, colToDo, "Move me");

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "Move me", description = "moved", columnId = colDone });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(colDone, doc.GetProperty("columnId").GetInt32());
    }

    [Fact]
    public async Task DeleteCard_ByMember_Returns204()
    {
        var token = await RegisterAndLoginAsync("card-delete@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId);
        var cardId = await CreateCardAsync(token, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}/cards/{cardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    // --- Authorization ---

    [Fact]
    public async Task CreateCard_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("card-owner@example.com");
        var outsiderToken = await RegisterAndLoginAsync("card-outsider@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/cards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        req.Content = JsonContent.Create(new { title = "Sneaky card", description = "", columnId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task UpdateCard_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("card-owner2@example.com");
        var outsiderToken = await RegisterAndLoginAsync("card-outsider2@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);
        var cardId = await CreateCardAsync(ownerToken, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        req.Content = JsonContent.Create(new { title = "Hijacked", description = "", columnId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("card-owner3@example.com");
        var outsiderToken = await RegisterAndLoginAsync("card-outsider3@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);
        var cardId = await CreateCardAsync(ownerToken, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}/cards/{cardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
