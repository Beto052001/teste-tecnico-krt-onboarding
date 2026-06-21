using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;

namespace KRT.Onboarding.Infrastructure.Caching;

/// <summary>
/// Cache "vazio" (sempre miss) usado até a etapa de cache entrar. Mantém a API funcional
/// de ponta a ponta sem alterar os casos de uso. Substituído pelo cache Redis na etapa 5.
/// </summary>
public sealed class NoOpContaCache : IContaCache
{
    public Task<ContaDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<ContaDto?>(null);

    public Task DefinirAsync(ContaDto conta, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
