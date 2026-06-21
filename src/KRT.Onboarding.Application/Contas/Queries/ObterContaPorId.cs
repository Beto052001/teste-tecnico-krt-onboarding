using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Domain.Contas;
using MediatR;

namespace KRT.Onboarding.Application.Contas.Queries;

public sealed record ObterContaPorIdQuery(Guid Id) : IRequest<ContaDto?>;

/// <summary>
/// Leitura por id com padrão cache-aside:
/// 1) tenta o cache (hit = nenhuma consulta ao banco, logo nenhum custo);
/// 2) no miss, consulta o banco e popula o cache para as próximas leituras do dia.
/// </summary>
public sealed class ObterContaPorIdQueryHandler : IRequestHandler<ObterContaPorIdQuery, ContaDto?>
{
    private readonly IContaRepository _repositorio;
    private readonly IContaCache _cache;

    public ObterContaPorIdQueryHandler(IContaRepository repositorio, IContaCache cache)
    {
        _repositorio = repositorio;
        _cache = cache;
    }

    public async Task<ContaDto?> Handle(ObterContaPorIdQuery request, CancellationToken cancellationToken)
    {
        var emCache = await _cache.ObterAsync(request.Id, cancellationToken);
        if (emCache is not null)
        {
            return emCache;
        }

        var conta = await _repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (conta is null)
        {
            return null;
        }

        var dto = ContaDto.De(conta);
        await _cache.DefinirAsync(dto, cancellationToken);
        return dto;
    }
}
