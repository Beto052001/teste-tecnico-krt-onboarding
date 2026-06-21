using FluentValidation;
using MediatR;

namespace KRT.Onboarding.Application.Common.Behaviors;

/// <summary>
/// Pipeline do MediatR que executa os validadores FluentValidation antes do handler.
/// Centraliza a validação de entrada: nenhum handler precisa validar manualmente.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var contexto = new ValidationContext<TRequest>(request);
        var falhas = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(contexto, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (falhas.Count != 0)
        {
            throw new ValidationException(falhas);
        }

        return await next();
    }
}
