using System.Text.Json;
using KRT.Onboarding.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>
/// Antes de salvar, transforma os eventos de domínio das entidades rastreadas em
/// <see cref="OutboxMessage"/> e os adiciona ao contexto — assim eles são persistidos
/// na MESMA transação da mudança do agregado (Transactional Outbox).
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ConverterEventosEmOutbox(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ConverterEventosEmOutbox(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void ConverterEventosEmOutbox(DbContext context)
    {
        var entidadesComEventos = context.ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var mensagens = new List<OutboxMessage>();

        foreach (var entidade in entidadesComEventos)
        {
            foreach (var evento in entidade.DomainEvents)
            {
                var tipo = evento.GetType().Name;
                var conteudo = JsonSerializer.Serialize(evento, evento.GetType(), JsonOptions);
                mensagens.Add(new OutboxMessage(tipo, conteudo, evento.OcorridoEmUtc));
            }

            entidade.ClearDomainEvents();
        }

        if (mensagens.Count > 0)
        {
            context.Set<OutboxMessage>().AddRange(mensagens);
        }
    }
}
