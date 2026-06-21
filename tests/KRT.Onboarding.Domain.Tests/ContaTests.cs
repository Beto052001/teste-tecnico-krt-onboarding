using FluentAssertions;
using KRT.Onboarding.Domain.Common;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Domain.Contas.Events;
using Xunit;

namespace KRT.Onboarding.Domain.Tests;

public class ContaTests
{
    private static Cpf CpfValido() => Cpf.Criar("52998224725");

    private static Conta ContaNova() => Conta.Criar("Titular Teste", CpfValido());

    [Fact]
    public void Criar_DeveNascerAtiva_NormalizarNome_EEmitirContaCriada()
    {
        var conta = Conta.Criar("  Roberto Marquini  ", CpfValido());

        conta.Id.Should().NotBeEmpty();
        conta.NomeTitular.Should().Be("Roberto Marquini");
        conta.Status.Should().Be(StatusConta.Ativa);
        conta.CriadaEmUtc.Should().Be(conta.AtualizadaEmUtc);
        conta.DomainEvents.Should().ContainSingle(e => e is ContaCriada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComNomeVazio_LancaDomainException(string? nome)
    {
        var acao = () => Conta.Criar(nome, CpfValido());

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_LancaDomainException()
    {
        var nomeLongo = new string('a', Conta.TamanhoMaximoNome + 1);

        var acao = () => Conta.Criar(nomeLongo, CpfValido());

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void AtualizarTitular_ComNomeDiferente_AlteraEEmiteContaAtualizada()
    {
        var conta = ContaNova();
        conta.ClearDomainEvents();

        conta.AtualizarTitular("Novo Nome");

        conta.NomeTitular.Should().Be("Novo Nome");
        conta.DomainEvents.Should().ContainSingle(e => e is ContaAtualizada);
    }

    [Fact]
    public void AtualizarTitular_ComMesmoNome_NaoEmiteEvento()
    {
        var conta = ContaNova();
        conta.ClearDomainEvents();

        conta.AtualizarTitular("Titular Teste");

        conta.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Inativar_QuandoAtiva_AlteraStatusEEmiteEvento()
    {
        var conta = ContaNova();
        conta.ClearDomainEvents();

        conta.Inativar();

        conta.Status.Should().Be(StatusConta.Inativa);
        conta.DomainEvents.Should().ContainSingle(e => e is ContaAtualizada);
    }

    [Fact]
    public void Inativar_QuandoJaInativa_EhIdempotente()
    {
        var conta = ContaNova();
        conta.Inativar();
        conta.ClearDomainEvents();

        conta.Inativar();

        conta.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarcarComoRemovida_EmiteContaRemovida()
    {
        var conta = ContaNova();
        conta.ClearDomainEvents();

        conta.MarcarComoRemovida();

        conta.DomainEvents.Should().ContainSingle(e => e is ContaRemovida);
    }
}
