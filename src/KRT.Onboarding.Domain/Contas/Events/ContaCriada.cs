using KRT.Onboarding.Domain.Common;

namespace KRT.Onboarding.Domain.Contas.Events;

/// <summary>Conta criada — consumida por áreas como prevenção a fraude e cartões.</summary>
public sealed record ContaCriada(Guid ContaId, string Cpf, string NomeTitular, StatusConta Status)
    : DomainEventBase;
