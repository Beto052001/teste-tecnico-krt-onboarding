namespace KRT.Onboarding.Domain.Common;

/// <summary>
/// Base para entidades. Concentra a coleção de eventos de domínio que a entidade
/// acumula ao longo das mudanças de estado e que serão despachados na persistência.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>Eventos pendentes de despacho (somente leitura).</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Limpa os eventos após o despacho (chamado pela infraestrutura).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
