using System.Reflection;
using FluentValidation;
using KRT.Onboarding.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace KRT.Onboarding.Application;

/// <summary>Registro da camada de aplicação: MediatR, validadores e pipeline de validação.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
