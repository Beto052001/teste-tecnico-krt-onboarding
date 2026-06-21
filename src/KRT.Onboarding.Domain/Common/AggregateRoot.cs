namespace KRT.Onboarding.Domain.Common;

/// <summary>
/// Raiz de agregado: única porta de entrada para alterar o estado do agregado e
/// fronteira de consistência transacional. Repositórios operam sobre raízes de agregado.
/// </summary>
public abstract class AggregateRoot : Entity
{
}
