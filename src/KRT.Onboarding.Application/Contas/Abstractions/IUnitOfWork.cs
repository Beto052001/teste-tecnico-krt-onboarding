namespace KRT.Onboarding.Application.Contas.Abstractions;

/// <summary>
/// Confirma, numa única transação, as mudanças do agregado e a publicação dos eventos
/// (via outbox). Abstrai o DbContext da camada de aplicação.
/// </summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
