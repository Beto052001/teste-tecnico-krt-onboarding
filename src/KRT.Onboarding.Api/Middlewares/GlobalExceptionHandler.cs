using FluentValidation;
using KRT.Onboarding.Application.Common.Exceptions;
using KRT.Onboarding.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KRT.Onboarding.Api.Middlewares;

/// <summary>
/// Converte exceções em respostas <c>ProblemDetails</c> (RFC 7807) com o status correto,
/// sem vazar detalhes internos em erros 500.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = MapearParaProblemDetails(exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erro não tratado.");
        }
        else
        {
            _logger.LogWarning("Falha tratada ({Status}): {Detail}", problem.Status, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails MapearParaProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => CriarValidacao(validation),
        DomainException domain => Criar(StatusCodes.Status400BadRequest, "Requisição inválida", domain.Message),
        NotFoundException notFound => Criar(StatusCodes.Status404NotFound, "Recurso não encontrado", notFound.Message),
        ConflictException conflict => Criar(StatusCodes.Status409Conflict, "Conflito", conflict.Message),
        _ => Criar(StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado."),
    };

    private static ProblemDetails Criar(int status, string titulo, string detalhe) => new()
    {
        Status = status,
        Title = titulo,
        Detail = detalhe,
    };

    private static ProblemDetails CriarValidacao(ValidationException exception)
    {
        var erros = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = Criar(
            StatusCodes.Status400BadRequest,
            "Erro de validação",
            "Um ou mais campos são inválidos.");
        problem.Extensions["errors"] = erros;
        return problem;
    }
}
