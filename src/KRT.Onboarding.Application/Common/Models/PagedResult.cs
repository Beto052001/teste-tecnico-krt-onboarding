namespace KRT.Onboarding.Application.Common.Models;

/// <summary>Resultado paginado genérico.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total)
{
    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);
}
