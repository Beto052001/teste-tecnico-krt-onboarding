using FluentValidation;
using KRT.Onboarding.Application.Common.Exceptions;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Domain.Contas;
using MediatR;

namespace KRT.Onboarding.Application.Contas.Commands;

public sealed record CriarContaCommand(string NomeTitular, string Cpf) : IRequest<ContaDto>;

public sealed class CriarContaCommandValidator : AbstractValidator<CriarContaCommand>
{
    public CriarContaCommandValidator()
    {
        RuleFor(x => x.NomeTitular)
            .NotEmpty().WithMessage("Nome do titular é obrigatório.")
            .MaximumLength(Conta.TamanhoMaximoNome);

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Must(Cpf.EhValido).WithMessage("CPF inválido.");
    }
}

public sealed class CriarContaCommandHandler : IRequestHandler<CriarContaCommand, ContaDto>
{
    private readonly IContaRepository _repositorio;
    private readonly IUnitOfWork _unitOfWork;

    public CriarContaCommandHandler(IContaRepository repositorio, IUnitOfWork unitOfWork)
    {
        _repositorio = repositorio;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContaDto> Handle(CriarContaCommand request, CancellationToken cancellationToken)
    {
        var cpf = Cpf.Criar(request.Cpf);

        if (await _repositorio.ExistePorCpfAsync(cpf.Valor, cancellationToken))
        {
            throw new ConflictException($"Já existe uma conta para o CPF {cpf.Formatado}.");
        }

        var conta = Conta.Criar(request.NomeTitular, cpf);

        await _repositorio.AdicionarAsync(conta, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ContaDto.De(conta);
    }
}
