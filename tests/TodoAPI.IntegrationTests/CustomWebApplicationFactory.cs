using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TodoApi.Models;

namespace TodoAPI.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    public CustomWebApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17.5-alpine3.22")
            .WithPortBinding(8080, true)
            .WithDatabase("test")
            .WithUsername("admin")
            .WithPassword("admin")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:8.0.3-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    public HttpClient HttpClient { get; set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        await ResetDatabaseAsync();
        await RunMigrationsAsync();

        HttpClient = CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgresContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
    }


    private async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        await context.Database.EnsureDeletedAsync();
    }

    private async Task RunMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Database migration failed during test setup", ex);
        }
    }
}