namespace KRT.Onboarding.Application.Common.Exceptions;

/// <summary>Conflito de estado (ex.: CPF já cadastrado). Traduzida para HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string mensagem) : base(mensagem)
    {
    }
}
