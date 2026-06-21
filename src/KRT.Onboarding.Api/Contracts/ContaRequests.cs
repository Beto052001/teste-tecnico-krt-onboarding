using KRT.Onboarding.Domain.Contas;

namespace KRT.Onboarding.Api.Contracts;

/// <summary>Corpo da requisição de criação de conta.</summary>
public sealed record CriarContaRequest(string NomeTitular, string Cpf);

/// <summary>Corpo da requisição de atualização de conta.</summary>
public sealed record AtualizarContaRequest(string NomeTitular, StatusConta Status);
