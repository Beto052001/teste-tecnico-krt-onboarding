using KRT.Onboarding.Infrastructure.Caching;
using KRT.Onboarding.Infrastructure.Messaging;
using KRT.Onboarding.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace KRT.Onboarding.IntegrationTests;

/// <summary>
/// Sobe Postgres e Redis reais em containers (Testcontainers) e aplica as migrations uma
/// única vez. Os testes batem na stack de verdade — repositório, mapeamentos, outbox e
/// cache-aside — em vez de mocks. Compartilhado por toda a coleção para subir o Docker só
/// uma vez; cada teste isola seus dados por Id/CPF próprios.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("krt_onboarding")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private string _postgresConnectionString = null!;
    private string _redisConnectionString = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        _postgresConnectionString = _postgres.GetConnectionString();
        _redisConnectionString = _redis.GetConnectionString();

        // Garante o schema aplicando as migrations versionadas (mesmo caminho de produção).
        await using var context = NewDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    /// <summary>
    /// Novo <see cref="OnboardingDbContext"/> com o <see cref="OutboxInterceptor"/> ligado —
    /// um por teste, para não vazar estado do change tracker entre cenários.
    /// </summary>
    public OnboardingDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<OnboardingDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .AddInterceptors(new OutboxInterceptor())
            .Options;

        return new OnboardingDbContext(options);
    }

    /// <summary>Cache de contas sobre o Redis real do container.</summary>
    public DistributedContaCache NewContaCache()
    {
        var redis = new RedisCache(Options.Create(new RedisCacheOptions
        {
            Configuration = _redisConnectionString,
        }));

        return new DistributedContaCache(redis, NullLogger<DistributedContaCache>.Instance);
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "integration";
}
