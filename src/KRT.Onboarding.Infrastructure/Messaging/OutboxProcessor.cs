using KRT.Onboarding.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>
/// Worker que drena a outbox: lê as mensagens não publicadas, envia ao EventBridge e as
/// marca como processadas. Mensagens que falham permanecem pendentes e são reprocessadas
/// no próximo ciclo (entrega "ao menos uma vez").
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);
    private const int TamanhoLote = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarLoteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no ciclo de processamento da outbox.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pendentes = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessadoEmUtc == null)
            .OrderBy(m => m.OcorridoEmUtc)
            .Take(TamanhoLote)
            .ToListAsync(cancellationToken);

        if (pendentes.Count == 0)
        {
            return;
        }

        foreach (var mensagem in pendentes)
        {
            try
            {
                await publisher.PublicarAsync(mensagem.Tipo, mensagem.Conteudo, cancellationToken);
                mensagem.MarcarComoProcessado(DateTime.UtcNow);
                _logger.LogInformation("Evento {Tipo} ({Id}) publicado no EventBridge.", mensagem.Tipo, mensagem.Id);
            }
            catch (Exception ex)
            {
                mensagem.RegistrarFalha(ex.Message);
                _logger.LogWarning(ex, "Falha ao publicar a mensagem {Id} ({Tipo}); será reprocessada.", mensagem.Id, mensagem.Tipo);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
