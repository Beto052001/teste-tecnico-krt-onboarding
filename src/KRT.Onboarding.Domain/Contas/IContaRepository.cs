namespace KRT.Onboarding.Domain.Contas;

/// <summary>
/// Repositório do agregado <see cref="Conta"/>. Orientado a coleção: não expõe
/// SaveChanges — a confirmação transacional é responsabilidade da Unit of Work.
/// </summary>
public interface IContaRepository
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistePorCpfAsync(string cpf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conta>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Conta conta, CancellationToken cancellationToken = default);

    void Atualizar(Conta conta);

    void Remover(Conta conta);
}
