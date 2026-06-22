using Amazon.EventBridge;
using Amazon.EventBridge.Model;

namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>
/// Publica eventos no Amazon EventBridge. O roteamento para cada área (prevenção a fraude,
/// cartões, ...) fica nas regras do barramento — a aplicação não conhece os consumidores.
/// </summary>
public sealed class EventBridgePublisher : IEventPublisher
{
    private readonly IAmazonEventBridge _client;
    private readonly MessagingOptions _options;

    public EventBridgePublisher(IAmazonEventBridge client, MessagingOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task PublicarAsync(string tipoEvento, string conteudoJson, CancellationToken cancellationToken = default)
    {
        var request = new PutEventsRequest
        {
            Entries =
            {
                new PutEventsRequestEntry
                {
                    EventBusName = _options.EventBusName,
                    Source = _options.EventSource,
                    DetailType = tipoEvento,
                    Detail = conteudoJson,
                },
            },
        };

        var response = await _client.PutEventsAsync(request, cancellationToken);

        if (response.FailedEntryCount > 0)
        {
            var motivo = response.Entries.FirstOrDefault()?.ErrorMessage ?? "motivo desconhecido";
            throw new InvalidOperationException($"EventBridge rejeitou o evento {tipoEvento}: {motivo}");
        }
    }
}
