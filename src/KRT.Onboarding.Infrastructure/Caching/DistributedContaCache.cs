using System.Text.Json;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace KRT.Onboarding.Infrastructure.Caching;

/// <summary>
/// Cache de contas sobre <see cref="IDistributedCache"/> (Redis/ElastiCache em produção).
/// Política: TTL até o fim do dia (UTC) — alinhada ao "já consultada naquele mesmo dia".
/// Falhas no cache são toleradas: a leitura degrada para o banco em vez de quebrar a API.
/// </summary>
public sealed class DistributedContaCache : IContaCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedContaCache> _logger;

    public DistributedContaCache(IDistributedCache cache, ILogger<DistributedContaCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<ContaDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(Chave(id), cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<ContaDto>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler o cache da conta {ContaId}; consultando o banco.", id);
            return null;
        }
    }

    public async Task DefinirAsync(ContaDto conta, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = FimDoDiaUtc() };
            var json = JsonSerializer.Serialize(conta, JsonOptions);
            await _cache.SetStringAsync(Chave(conta.Id), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar o cache da conta {ContaId}.", conta.Id);
        }
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(Chave(id), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao invalidar o cache da conta {ContaId}.", id);
        }
    }

    private static string Chave(Guid id) => $"conta:{id}";

    /// <summary>Próxima meia-noite UTC — entradas criadas hoje expiram no virar do dia.</summary>
    private static DateTimeOffset FimDoDiaUtc() =>
        new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(1);
}
