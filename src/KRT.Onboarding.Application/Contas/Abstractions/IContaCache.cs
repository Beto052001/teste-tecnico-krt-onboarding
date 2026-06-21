using KRT.Onboarding.Application.Contas.Dtos;

namespace KRT.Onboarding.Application.Contas.Abstractions;

/// <summary>
/// Cache de contas (padrão cache-aside). A política de expiração — TTL até o fim do dia,
/// alinhada ao "já consultada naquele mesmo dia" do enunciado — fica na implementação.
/// </summary>
public interface IContaCache
{
    Task<ContaDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default);

    Task DefinirAsync(ContaDto conta, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
