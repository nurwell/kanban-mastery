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

public class CardAssignTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CardAssignTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("CardAssignTestDb"));
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

    private async Task<string> GetUserIdAsync(string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;
    }

    private async Task<int> CreateBoardAsync(string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { boardName = "Assign Test Board" });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    private async Task AddMemberAsync(string ownerToken, int boardId, string memberId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/members");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        req.Content = JsonContent.Create(new { userId = memberId });
        await _client.SendAsync(req);
    }

    private async Task<int> CreateColumnAsync(string token, int boardId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/columns");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "To Do" });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateCardAsync(string token, int boardId, int columnId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/cards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { title = "Task", description = "desc", columnId });
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task AssignCard_ToValidMember_Returns200WithAssignedUserId()
    {
        var ownerToken = await RegisterAndLoginAsync("assign-owner@example.com");
        var memberToken = await RegisterAndLoginAsync("assign-member@example.com");
        var memberId = await GetUserIdAsync(memberToken);

        var boardId = await CreateBoardAsync(ownerToken);
        await AddMemberAsync(ownerToken, boardId, memberId);
        var columnId = await CreateColumnAsync(ownerToken, boardId);
        var cardId = await CreateCardAsync(ownerToken, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}/assign");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        req.Content = JsonContent.Create(new { userId = memberId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(memberId, doc.GetProperty("assignedToUserId").GetString());
    }

    [Fact]
    public async Task AssignCard_ToNonMember_Returns400()
    {
        var ownerToken = await RegisterAndLoginAsync("assign-owner2@example.com");
        var outsiderToken = await RegisterAndLoginAsync("assign-outsider@example.com");
        var outsiderId = await GetUserIdAsync(outsiderToken);

        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);
        var cardId = await CreateCardAsync(ownerToken, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}/assign");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        req.Content = JsonContent.Create(new { userId = outsiderId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("not a board member", body);
    }

    [Fact]
    public async Task AssignCard_ByNonMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("assign-owner3@example.com");
        var outsiderToken = await RegisterAndLoginAsync("assign-outsider2@example.com");
        var ownerId = await GetUserIdAsync(ownerToken);

        var boardId = await CreateBoardAsync(ownerToken);
        var columnId = await CreateColumnAsync(ownerToken, boardId);
        var cardId = await CreateCardAsync(ownerToken, boardId, columnId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}/cards/{cardId}/assign");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        req.Content = JsonContent.Create(new { userId = ownerId });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
