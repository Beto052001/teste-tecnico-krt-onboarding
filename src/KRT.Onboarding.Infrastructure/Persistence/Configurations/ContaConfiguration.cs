using KRT.Onboarding.Domain.Contas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRT.Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento da entidade <see cref="Conta"/> para a tabela "contas".</summary>
public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("contas");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // o Id é gerado no domínio, não pelo banco

        builder.Property(c => c.NomeTitular)
            .HasColumnName("nome_titular")
            .HasMaxLength(Conta.TamanhoMaximoNome)
            .IsRequired();

        // CPF como objeto de valor (owned): vira a coluna "cpf" e permite consulta por valor.
        builder.OwnsOne(c => c.Cpf, cpf =>
        {
            cpf.Property(p => p.Valor)
                .HasColumnName("cpf")
                .HasMaxLength(Cpf.Tamanho)
                .IsFixedLength()
                .IsRequired();

            cpf.HasIndex(p => p.Valor)
                .IsUnique()
                .HasDatabaseName("ux_contas_cpf");
        });
        builder.Navigation(c => c.Cpf).IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>() // persiste o enum como texto legível
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CriadaEmUtc).HasColumnName("criada_em_utc").IsRequired();
        builder.Property(c => c.AtualizadaEmUtc).HasColumnName("atualizada_em_utc").IsRequired();

        // Eventos de domínio não são colunas.
        builder.Ignore(c => c.DomainEvents);
    }
}
