using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Infrastructure.Caching;
using KRT.Onboarding.Infrastructure.Persistence;
using KRT.Onboarding.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KRT.Onboarding.Infrastructure;

/// <summary>Registro da camada de infraestrutura (persistência, cache; eventos na etapa 6).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        services.AddDbContext<OnboardingDbContext>(options => options.UseNpgsql(connectionString));

        // O DbContext é a Unit of Work.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OnboardingDbContext>());
        services.AddScoped<IContaRepository, ContaRepository>();

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            // Produção: Amazon ElastiCache for Redis.
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }
        else
        {
            // Sem Redis (testes/dev mínimo): cache distribuído em memória — mesma abstração.
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IContaCache, DistributedContaCache>();
        return services;
    }
}
