using FluentValidation;
using KRT.Onboarding.Application.Common.Models;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Domain.Contas;
using MediatR;

namespace KRT.Onboarding.Application.Contas.Queries;

public sealed record ListarContasQuery(int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<PagedResult<ContaDto>>;

public sealed class ListarContasQueryValidator : AbstractValidator<ListarContasQuery>
{
    public ListarContasQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
    }
}

public sealed class ListarContasQueryHandler : IRequestHandler<ListarContasQuery, PagedResult<ContaDto>>
{
    private readonly IContaRepository _repositorio;

    public ListarContasQueryHandler(IContaRepository repositorio) => _repositorio = repositorio;

    public async Task<PagedResult<ContaDto>> Handle(ListarContasQuery request, CancellationToken cancellationToken)
    {
        var contas = await _repositorio.ListarAsync(request.Pagina, request.TamanhoPagina, cancellationToken);
        var total = await _repositorio.ContarAsync(cancellationToken);

        var itens = contas.Select(ContaDto.De).ToList();
        return new PagedResult<ContaDto>(itens, request.Pagina, request.TamanhoPagina, total);
    }
}
