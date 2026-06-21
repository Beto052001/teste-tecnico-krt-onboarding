using KRT.Onboarding.Domain.Common;

namespace KRT.Onboarding.Domain.Contas.Events;

/// <summary>Conta removida — áreas consumidoras devem encerrar processos vinculados.</summary>
public sealed record ContaRemovida(Guid ContaId, string Cpf) : DomainEventBase;
