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

public class ColumnTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ColumnTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("ColumnTestDb"));
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
        req.Content = JsonContent.Create(new { name = "Column Test Board" });
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

    // --- Happy path ---

    [Fact]
    public async Task CreateColumn_ByMember_Returns201()
    {
        var token = await RegisterAndLoginAsync("col-create@example.com");
        var boardId = await CreateBoardAsync(token);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/columns");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "In Progress" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal("In Progress", doc.GetProperty("title").GetString());
        Assert.Equal(boardId, doc.GetProperty("boardId").GetInt32());
    }

    [Fact]
    public async Task UpdateColumn_ByMember_Returns200WithNewTitle()
    {
        var token = await RegisterAndLoginAsync("col-update@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/columns/{columnId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "Renamed" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Renamed", body);
    }

    [Fact]
    public async Task DeleteColumn_ByMember_NoCards_Returns204()
    {
        var token = await RegisterAndLoginAsync("col-delete@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}/columns/{columnId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_WithCards_Returns400()
    {
        var token = await RegisterAndLoginAsync("col-delete-cards@example.com");
        var boardId = await CreateBoardAsync(token);
        var columnId = await CreateColumnAsync(token, boardId);

        // Seed a card directly in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Cards.Add(new KanbanApi.Models.Card
            {
                Title = "Blocking card",
                Description = "Prevents deletion",
                Position = 0,
                ColumnId = columnId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}/columns/{columnId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Cannot delete column with existing cards", body);
    }

    // --- Authorization ---

    [Fact]
    public async Task CreateColumn_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("col-owner@example.com");
        var outsiderToken = await RegisterAndLoginAsync("col-outsider@example.com");
        var boardId = await CreateBoardAsync(ownerToken);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/columns");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        req.Content = JsonContent.Create(new { title = "Sneaky Column" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task UpdateColumn_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("col-owner2@example.com");
        var outsiderToken = await RegisterAndLoginAsync("col-outsider2@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/columns/{columnId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        req.Content = JsonContent.Create(new { title = "Hijacked" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("col-owner3@example.com");
        var outsiderToken = await RegisterAndLoginAsync("col-outsider3@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}/columns/{columnId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
