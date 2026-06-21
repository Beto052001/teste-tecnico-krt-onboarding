namespace KRT.Onboarding.Domain.Common;

/// <summary>Base para eventos de domínio; carimba o instante de ocorrência em UTC.</summary>
public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OcorridoEmUtc { get; init; } = DateTime.UtcNow;
}
