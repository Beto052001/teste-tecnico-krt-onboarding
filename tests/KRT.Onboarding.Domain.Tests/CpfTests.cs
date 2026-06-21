using FluentAssertions;
using KRT.Onboarding.Domain.Common;
using KRT.Onboarding.Domain.Contas;
using Xunit;

namespace KRT.Onboarding.Domain.Tests;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData("  529.982.247-25  ", "52998224725")]
    public void Criar_ComCpfValido_NormalizaParaSomenteDigitos(string entrada, string esperado)
    {
        var cpf = Cpf.Criar(entrada);

        cpf.Valor.Should().Be(esperado);
    }

    [Fact]
    public void Formatado_DeveAplicarMascara()
    {
        Cpf.Criar("52998224725").Formatado.Should().Be("529.982.247-25");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("5299822472")]      // 10 dígitos
    [InlineData("529982247250")]    // 12 dígitos
    [InlineData("12345678900")]     // dígitos verificadores inválidos
    [InlineData("11111111111")]     // todos iguais
    [InlineData("00000000000")]
    public void Criar_ComCpfInvalido_LancaDomainException(string? entrada)
    {
        var acao = () => Cpf.Criar(entrada);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cpfs_ComMesmoValor_SaoIguais()
    {
        Cpf.Criar("529.982.247-25").Should().Be(Cpf.Criar("52998224725"));
    }
}
