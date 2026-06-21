namespace KRT.Onboarding.Domain.Common;

/// <summary>
/// Exceção para violações de invariantes de domínio (regras de negócio).
/// É traduzida para HTTP 400/422 na borda da API, sem vazar detalhes internos.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public static void Requer(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new DomainException(mensagem);
        }
    }
}
