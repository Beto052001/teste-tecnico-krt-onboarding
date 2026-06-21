using KRT.Onboarding.Domain.Contas;

namespace KRT.Onboarding.Application.Contas.Dtos;

/// <summary>Representação de saída de uma conta (CPF mascarado, status como texto).</summary>
public sealed record ContaDto(Guid Id, string NomeTitular, string Cpf, string Status)
{
    public static ContaDto De(Conta conta) => new(
        conta.Id,
        conta.NomeTitular,
        conta.Cpf.Formatado,
        conta.Status.ToString());
}
