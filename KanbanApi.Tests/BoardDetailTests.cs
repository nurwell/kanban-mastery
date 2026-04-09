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

public class BoardDetailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardDetailTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("BoardDetailTestDb"));
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

    private async Task<int> CreateBoardAsync(string token, string boardName = "Detail Board")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { name = boardName });
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetBoard_ByMember_ReturnsBoardWithColumnsAndCards()
    {
        // Arrange — owner creates board; seed columns + cards directly in DB
        var ownerToken = await RegisterAndLoginAsync("detail-owner@example.com");
        var boardId = await CreateBoardAsync(ownerToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var col = new KanbanApi.Models.Column { Title = "To Do", Position = 0, BoardId = boardId };
            db.Columns.Add(col);
            await db.SaveChangesAsync();

            db.Cards.Add(new KanbanApi.Models.Card
            {
                Title = "First card",
                Description = "Do the thing",
                Position = 0,
                ColumnId = col.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/boards/{boardId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.SendAsync(request);

        // Assert — 200 with nested structure
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;

        Assert.Equal(boardId, doc.GetProperty("id").GetInt32());
        var columns = doc.GetProperty("columns");
        Assert.Equal(1, columns.GetArrayLength());
        var cards = columns[0].GetProperty("cards");
        Assert.Equal(1, cards.GetArrayLength());
        Assert.Equal("First card", cards[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetBoard_ByNonMember_ReturnsForbidden()
    {
        // Arrange
        var ownerToken = await RegisterAndLoginAsync("detail-owner2@example.com");
        var outsiderToken = await RegisterAndLoginAsync("outsider@example.com");
        var boardId = await CreateBoardAsync(ownerToken);

        // Act — outsider tries to fetch
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/boards/{boardId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
