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

public class BoardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("BoardTestDb"));
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

    [Fact]
    public async Task CreateBoard_WithValidToken_CreatesBoardAndMembershipRecords()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("boardowner@example.com");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { name = "My Test Board" });

        // Act
        var response = await _client.SendAsync(request);

        // Assert — HTTP 201
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var boardId = JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();

        // Assert — both records exist in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var board = await db.Boards.FindAsync(boardId);
        Assert.NotNull(board);
        Assert.Equal("My Test Board", board.Name);

        var membership = await db.BoardMembers
            .FirstOrDefaultAsync(bm => bm.BoardId == boardId);
        Assert.NotNull(membership);
        Assert.Equal("Owner", membership.Role);
        Assert.Equal(board.OwnerId, membership.UserId);
    }

    [Fact]
    public async Task CreateBoard_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/boards", new { name = "Unauthorized Board" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBoards_ReturnsOnlyUsersBoards()
    {
        var tokenA = await RegisterAndLoginAsync("list-userA@example.com");
        var tokenB = await RegisterAndLoginAsync("list-userB@example.com");

        var reqA = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        reqA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        reqA.Content = JsonContent.Create(new { name = "Board A" });
        await _client.SendAsync(reqA);

        var reqB = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        reqB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        reqB.Content = JsonContent.Create(new { name = "Board B" });
        await _client.SendAsync(reqB);

        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/boards");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var listRes = await _client.SendAsync(listReq);

        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var body = await listRes.Content.ReadAsStringAsync();
        Assert.Contains("Board A", body);
        Assert.DoesNotContain("Board B", body);
    }
}
