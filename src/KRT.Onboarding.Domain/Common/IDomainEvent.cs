namespace KRT.Onboarding.Domain.Common;

/// <summary>
/// Marcador para eventos de domínio. Mantido sem dependências externas (ex.: MediatR)
/// para preservar a camada de domínio pura. A "ponte" para a mensageria fica na infra.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Momento (UTC) em que o fato de negócio ocorreu.</summary>
    DateTime OcorridoEmUtc { get; }
}
