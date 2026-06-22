using KRT.Onboarding.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRT.Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento da tabela "outbox_messages".</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.Tipo).HasColumnName("tipo").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Conteudo).HasColumnName("conteudo").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OcorridoEmUtc).HasColumnName("ocorrido_em_utc").IsRequired();
        builder.Property(m => m.ProcessadoEmUtc).HasColumnName("processado_em_utc");
        builder.Property(m => m.Tentativas).HasColumnName("tentativas").HasDefaultValue(0);
        builder.Property(m => m.UltimoErro).HasColumnName("ultimo_erro");

        // Consulta do worker: pega as não processadas em ordem de ocorrência.
        builder.HasIndex(m => new { m.ProcessadoEmUtc, m.OcorridoEmUtc })
            .HasDatabaseName("ix_outbox_pendentes");
    }
}
