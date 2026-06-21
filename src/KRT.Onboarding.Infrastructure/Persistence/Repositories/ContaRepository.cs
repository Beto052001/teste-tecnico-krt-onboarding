using KRT.Onboarding.Domain.Contas;
using Microsoft.EntityFrameworkCore;

namespace KRT.Onboarding.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do <see cref="IContaRepository"/>.</summary>
public sealed class ContaRepository : IContaRepository
{
    private readonly OnboardingDbContext _context;

    public ContaRepository(OnboardingDbContext context) => _context = context;

    public Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Contas.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistePorCpfAsync(string cpf, CancellationToken cancellationToken = default) =>
        _context.Contas.AsNoTracking().AnyAsync(c => c.Cpf.Valor == cpf, cancellationToken);

    public async Task<IReadOnlyList<Conta>> ListarAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        return await _context.Contas
            .AsNoTracking()
            .OrderByDescending(c => c.CriadaEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarAsync(CancellationToken cancellationToken = default) =>
        _context.Contas.CountAsync(cancellationToken);

    public async Task AdicionarAsync(Conta conta, CancellationToken cancellationToken = default) =>
        await _context.Contas.AddAsync(conta, cancellationToken);

    public void Atualizar(Conta conta) => _context.Contas.Update(conta);

    public void Remover(Conta conta) => _context.Contas.Remove(conta);
}
