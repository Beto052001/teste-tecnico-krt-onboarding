namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>Publica um evento de integração no barramento (Amazon EventBridge).</summary>
public interface IEventPublisher
{
    Task PublicarAsync(string tipoEvento, string conteudoJson, CancellationToken cancellationToken = default);
}
