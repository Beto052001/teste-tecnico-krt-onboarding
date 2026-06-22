using Amazon.EventBridge;
using Amazon.Runtime;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Infrastructure.Caching;
using KRT.Onboarding.Infrastructure.Messaging;
using KRT.Onboarding.Infrastructure.Persistence;
using KRT.Onboarding.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KRT.Onboarding.Infrastructure;

/// <summary>Registro da camada de infraestrutura: persistência, cache e mensageria.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        services.AddMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        // Interceptor grava os eventos de domínio na outbox na mesma transação do SaveChanges.
        services.AddSingleton<OutboxInterceptor>();
        services.AddDbContext<OnboardingDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<OutboxInterceptor>()));

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

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MessagingOptions.SectionName);
        var options = new MessagingOptions
        {
            ServiceUrl = section["ServiceUrl"],
            Region = section["Region"] ?? "us-east-1",
            EventBusName = section["EventBusName"] ?? "krt-onboarding-bus",
            EventSource = section["EventSource"] ?? "krt.onboarding",
        };
        services.AddSingleton(options);

        services.AddSingleton<IAmazonEventBridge>(_ => CriarEventBridgeClient(options));
        services.AddScoped<IEventPublisher, EventBridgePublisher>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    private static IAmazonEventBridge CriarEventBridgeClient(MessagingOptions options)
    {
        var config = new AmazonEventBridgeConfig();

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            // LocalStack: endpoint custom + credenciais fictícias.
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
            return new AmazonEventBridgeClient(new BasicAWSCredentials("test", "test"), config);
        }

        // AWS real: credenciais pela cadeia padrão (IAM role/perfil).
        config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
        return new AmazonEventBridgeClient(config);
    }
}
