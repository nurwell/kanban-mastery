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

public class BoardOwnerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardOwnerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("BoardOwnerTestDb"));
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
        req.Content = JsonContent.Create(new { name = "Owner Test Board" });
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

    // --- Happy path ---

    [Fact]
    public async Task UpdateBoard_ByOwner_Returns200WithNewName()
    {
        var token = await RegisterAndLoginAsync("board-update-owner@example.com");
        var boardId = await CreateBoardAsync(token);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { name = "Renamed Board" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Renamed Board", body);
    }

    [Fact]
    public async Task DeleteBoard_ByOwner_Returns204()
    {
        var token = await RegisterAndLoginAsync("board-delete-owner@example.com");
        var boardId = await CreateBoardAsync(token);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    // --- Authorization ---

    [Fact]
    public async Task UpdateBoard_ByMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("board-upd-owner@example.com");
        var memberToken = await RegisterAndLoginAsync("board-upd-member@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var memberId = await GetUserIdAsync(memberToken);
        await AddMemberAsync(ownerToken, boardId, memberId);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/boards/{boardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        req.Content = JsonContent.Create(new { name = "Member Rename Attempt" });
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteBoard_ByMember_Returns403()
    {
        var ownerToken = await RegisterAndLoginAsync("board-del-owner@example.com");
        var memberToken = await RegisterAndLoginAsync("board-del-member@example.com");
        var boardId = await CreateBoardAsync(ownerToken);
        var memberId = await GetUserIdAsync(memberToken);
        await AddMemberAsync(ownerToken, boardId, memberId);

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/boards/{boardId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
