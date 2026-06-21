using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KRT.Onboarding.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada apenas em tempo de design pelo <c>dotnet ef</c> para criar o contexto
/// ao gerar/aplicar migrations, sem precisar subir a API. A string pode ser sobrescrita
/// pela variável de ambiente CONNECTIONSTRINGS__POSTGRES.
/// </summary>
public sealed class OnboardingDbContextFactory : IDesignTimeDbContextFactory<OnboardingDbContext>
{
    public OnboardingDbContext CreateDbContext(string[] args)
    {
        // Porta 55432 (host) aponta para o container ISOLADO deste projeto — nunca para
        // outros Postgres já em execução na máquina (ex.: 5432/5433).
        var connectionString =
            Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__POSTGRES")
            ?? "Host=localhost;Port=55432;Database=krt_onboarding;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OnboardingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OnboardingDbContext(options);
    }
}
