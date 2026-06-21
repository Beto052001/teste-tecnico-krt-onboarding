using KRT.Onboarding.Application.Common.Exceptions;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Domain.Contas;
using MediatR;

namespace KRT.Onboarding.Application.Contas.Commands;

public sealed record RemoverContaCommand(Guid Id) : IRequest;

public sealed class RemoverContaCommandHandler : IRequestHandler<RemoverContaCommand>
{
    private readonly IContaRepository _repositorio;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContaCache _cache;

    public RemoverContaCommandHandler(
        IContaRepository repositorio,
        IUnitOfWork unitOfWork,
        IContaCache cache)
    {
        _repositorio = repositorio;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(RemoverContaCommand request, CancellationToken cancellationToken)
    {
        var conta = await _repositorio.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Conta", request.Id);

        // Emite ContaRemovida antes do descarte (capturada pela outbox na persistência).
        conta.MarcarComoRemovida();

        _repositorio.Remover(conta);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _cache.RemoverAsync(conta.Id, cancellationToken);
    }
}
