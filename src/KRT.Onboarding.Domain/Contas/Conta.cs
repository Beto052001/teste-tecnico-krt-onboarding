using KRT.Onboarding.Domain.Common;
using KRT.Onboarding.Domain.Contas.Events;

namespace KRT.Onboarding.Domain.Contas;

/// <summary>
/// Conta de cliente — raiz de agregado. O estado só muda por métodos de negócio, que
/// garantem invariantes e emitem eventos de domínio para que as demais áreas reajam.
/// </summary>
public sealed class Conta : AggregateRoot
{
    public const int TamanhoMaximoNome = 150;

    public Guid Id { get; private set; }
    public string NomeTitular { get; private set; } = null!;
    public Cpf Cpf { get; private set; } = null!;
    public StatusConta Status { get; private set; }
    public DateTime CriadaEmUtc { get; private set; }
    public DateTime AtualizadaEmUtc { get; private set; }

    // Exigido pelo EF Core para materialização; não usar no domínio.
    private Conta()
    {
    }

    private Conta(Guid id, string nomeTitular, Cpf cpf, StatusConta status, DateTime agoraUtc)
    {
        Id = id;
        NomeTitular = nomeTitular;
        Cpf = cpf;
        Status = status;
        CriadaEmUtc = agoraUtc;
        AtualizadaEmUtc = agoraUtc;
    }

    /// <summary>Cria uma conta nova (nasce Ativa) e registra o evento <see cref="ContaCriada"/>.</summary>
    public static Conta Criar(string? nomeTitular, Cpf cpf)
    {
        var nome = NormalizarNome(nomeTitular);
        var conta = new Conta(Guid.NewGuid(), nome, cpf, StatusConta.Ativa, DateTime.UtcNow);
        conta.RaiseDomainEvent(new ContaCriada(conta.Id, cpf.Valor, nome, conta.Status));
        return conta;
    }

    /// <summary>Atualiza o nome do titular.</summary>
    public void AtualizarTitular(string? novoNome)
    {
        var nome = NormalizarNome(novoNome);
        if (nome == NomeTitular)
        {
            return;
        }

        NomeTitular = nome;
        Tocar();
    }

    /// <summary>Ativa a conta (idempotente).</summary>
    public void Ativar()
    {
        if (Status == StatusConta.Ativa)
        {
            return;
        }

        Status = StatusConta.Ativa;
        Tocar();
    }

    /// <summary>Inativa a conta (idempotente).</summary>
    public void Inativar()
    {
        if (Status == StatusConta.Inativa)
        {
            return;
        }

        Status = StatusConta.Inativa;
        Tocar();
    }

    /// <summary>Aplica um status arbitrário (usado pelo caso de uso de atualização).</summary>
    public void DefinirStatus(StatusConta status)
    {
        if (status == StatusConta.Ativa)
        {
            Ativar();
        }
        else
        {
            Inativar();
        }
    }

    /// <summary>Registra a remoção, emitindo <see cref="ContaRemovida"/> antes do descarte.</summary>
    public void MarcarComoRemovida() =>
        RaiseDomainEvent(new ContaRemovida(Id, Cpf.Valor));

    private void Tocar()
    {
        AtualizadaEmUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ContaAtualizada(Id, Cpf.Valor, NomeTitular, Status));
    }

    private static string NormalizarNome(string? nome)
    {
        DomainException.Requer(!string.IsNullOrWhiteSpace(nome), "Nome do titular é obrigatório.");
        var normalizado = nome!.Trim();
        DomainException.Requer(
            normalizado.Length <= TamanhoMaximoNome,
            $"Nome do titular não pode exceder {TamanhoMaximoNome} caracteres.");
        return normalizado;
    }
}
