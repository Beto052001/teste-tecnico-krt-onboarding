using KRT.Onboarding.Domain.Common;

namespace KRT.Onboarding.Domain.Contas.Events;

/// <summary>Conta atualizada (titular e/ou status).</summary>
public sealed record ContaAtualizada(Guid ContaId, string Cpf, string NomeTitular, StatusConta Status)
    : DomainEventBase;
