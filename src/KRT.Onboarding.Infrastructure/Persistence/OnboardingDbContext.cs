using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Domain.Contas;
using Microsoft.EntityFrameworkCore;

namespace KRT.Onboarding.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core. Também implementa <see cref="IUnitOfWork"/>: o commit das mudanças
/// do agregado é o ponto onde, futuramente, os eventos vão para a outbox na mesma transação.
/// </summary>
public sealed class OnboardingDbContext : DbContext, IUnitOfWork
{
    public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options) : base(options)
    {
    }

    public DbSet<Conta> Contas => Set<Conta>();

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OnboardingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
