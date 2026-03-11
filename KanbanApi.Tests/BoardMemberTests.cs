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

public class BoardMemberTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardMemberTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("BoardMemberTestDb"));
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
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;
    }

    private async Task<int> CreateBoardAsync(string token, string boardName = "Test Board")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/boards");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { boardName });
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task AddMember_ByOwner_CreatesMemberRecord()
    {
        // Arrange
        var ownerToken = await RegisterAndLoginAsync("owner@members.com");
        var memberToken = await RegisterAndLoginAsync("member@members.com");
        var memberId = await GetUserIdAsync(memberToken);
        var boardId = await CreateBoardAsync(ownerToken);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/members");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        request.Content = JsonContent.Create(new { userId = memberId });
        var response = await _client.SendAsync(request);

        // Assert — HTTP 201
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Assert — BoardMember record exists with role "Member"
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.BoardMembers
            .FirstOrDefaultAsync(bm => bm.BoardId == boardId && bm.UserId == memberId);

        Assert.NotNull(membership);
        Assert.Equal("Member", membership.Role);
    }

    [Fact]
    public async Task AddMember_ByNonOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerToken = await RegisterAndLoginAsync("owner2@members.com");
        var nonOwnerToken = await RegisterAndLoginAsync("nonowner@members.com");
        var boardId = await CreateBoardAsync(ownerToken);

        // Act — non-owner tries to add a member
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/boards/{boardId}/members");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nonOwnerToken);
        request.Content = JsonContent.Create(new { userId = "someUserId" });
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
