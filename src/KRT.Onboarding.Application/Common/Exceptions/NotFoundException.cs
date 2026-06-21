namespace KRT.Onboarding.Application.Common.Exceptions;

/// <summary>Recurso não encontrado. Traduzida para HTTP 404 na borda da API.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string recurso, object chave)
        : base($"{recurso} '{chave}' não encontrado(a).")
    {
    }
}
