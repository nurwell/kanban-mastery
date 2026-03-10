using System.Net;
using System.Net.Http.Json;
using KanbanApi.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KanbanApi.Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real SQLite DB with in-memory DB for tests
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                             || d.ServiceType == typeof(ApplicationDbContext)
                             || (d.ServiceType.IsGenericType &&
                                 d.ServiceType.GetGenericTypeDefinition().FullName!
                                     .Contains("IDbContextOptionsConfiguration")))
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();

        // Ensure in-memory database schema is created
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/register", new
        {
            email = "test@example.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange — register first
        await _client.PostAsJsonAsync("/register", new
        {
            email = "login@example.com",
            password = "Test123!"
        });

        // Act — login
        var response = await _client.PostAsJsonAsync("/login", new
        {
            email = "login@example.com",
            password = "Test123!"
        });

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("accessToken", body);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var payload = new
        {
            email = "duplicate@example.com",
            password = "Test123!"
        };

        // First registration
        await _client.PostAsJsonAsync("/register", payload);

        // Duplicate registration
        var response = await _client.PostAsJsonAsync("/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
