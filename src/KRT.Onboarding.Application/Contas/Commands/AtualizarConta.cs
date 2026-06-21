using FluentValidation;
using KRT.Onboarding.Application.Common.Exceptions;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Domain.Contas;
using MediatR;

namespace KRT.Onboarding.Application.Contas.Commands;

public sealed record AtualizarContaCommand(Guid Id, string NomeTitular, StatusConta Status) : IRequest<ContaDto>;

public sealed class AtualizarContaCommandValidator : AbstractValidator<AtualizarContaCommand>
{
    public AtualizarContaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.NomeTitular)
            .NotEmpty().WithMessage("Nome do titular é obrigatório.")
            .MaximumLength(Conta.TamanhoMaximoNome);

        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");
    }
}

public sealed class AtualizarContaCommandHandler : IRequestHandler<AtualizarContaCommand, ContaDto>
{
    private readonly IContaRepository _repositorio;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContaCache _cache;

    public AtualizarContaCommandHandler(
        IContaRepository repositorio,
        IUnitOfWork unitOfWork,
        IContaCache cache)
    {
        _repositorio = repositorio;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<ContaDto> Handle(AtualizarContaCommand request, CancellationToken cancellationToken)
    {
        var conta = await _repositorio.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Conta", request.Id);

        conta.AtualizarTitular(request.NomeTitular);
        conta.DefinirStatus(request.Status);

        _repositorio.Atualizar(conta);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Invalida o cache para nunca servir dado velho após a escrita.
        await _cache.RemoverAsync(conta.Id, cancellationToken);

        return ContaDto.De(conta);
    }
}
