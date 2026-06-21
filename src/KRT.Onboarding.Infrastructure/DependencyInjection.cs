using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Infrastructure.Caching;
using KRT.Onboarding.Infrastructure.Persistence;
using KRT.Onboarding.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KRT.Onboarding.Infrastructure;

/// <summary>Registro da camada de infraestrutura (persistência; cache e eventos nas etapas seguintes).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);

        // Etapa 5 substitui por um cache Redis (cache-aside com TTL diário).
        services.AddScoped<IContaCache, NoOpContaCache>();

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
}
