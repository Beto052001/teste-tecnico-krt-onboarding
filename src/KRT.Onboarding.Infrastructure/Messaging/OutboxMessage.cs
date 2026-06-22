namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>
/// Mensagem da Transactional Outbox: o evento de domínio é gravado na mesma transação da
/// mudança do agregado (evita o problema de dual-write) e publicado depois pelo worker.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string Conteudo { get; private set; } = null!;
    public DateTime OcorridoEmUtc { get; private set; }
    public DateTime? ProcessadoEmUtc { get; private set; }
    public int Tentativas { get; private set; }
    public string? UltimoErro { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string tipo, string conteudo, DateTime ocorridoEmUtc)
    {
        Id = Guid.NewGuid();
        Tipo = tipo;
        Conteudo = conteudo;
        OcorridoEmUtc = ocorridoEmUtc;
    }

    public void MarcarComoProcessado(DateTime processadoEmUtc)
    {
        ProcessadoEmUtc = processadoEmUtc;
        UltimoErro = null;
    }

    public void RegistrarFalha(string erro)
    {
        Tentativas++;
        UltimoErro = erro;
    }
}
