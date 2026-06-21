using FluentAssertions;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Application.Contas.Queries;
using KRT.Onboarding.Domain.Contas;
using NSubstitute;
using Xunit;

namespace KRT.Onboarding.Application.Tests.Contas;

public class ContaQueryHandlerTests
{
    private const string CpfValido = "52998224725";

    private readonly IContaRepository _repositorio = Substitute.For<IContaRepository>();
    private readonly IContaCache _cache = Substitute.For<IContaCache>();

    [Fact]
    public async Task ObterPorId_ComCacheHit_RetornaDoCache_ENaoConsultaBanco()
    {
        var id = Guid.NewGuid();
        var emCache = new ContaDto(id, "Roberto", "529.982.247-25", "Ativa");
        _cache.ObterAsync(id, Arg.Any<CancellationToken>()).Returns(emCache);
        var handler = new ObterContaPorIdQueryHandler(_repositorio, _cache);

        var resultado = await handler.Handle(new ObterContaPorIdQuery(id), default);

        resultado.Should().Be(emCache);
        await _repositorio.DidNotReceive().ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObterPorId_ComCacheMiss_ConsultaBancoEPopulaCache()
    {
        var conta = Conta.Criar("Roberto", Cpf.Criar(CpfValido));
        _cache.ObterAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ContaDto?)null);
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(conta);
        var handler = new ObterContaPorIdQueryHandler(_repositorio, _cache);

        var resultado = await handler.Handle(new ObterContaPorIdQuery(conta.Id), default);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(conta.Id);
        await _cache.Received(1).DefinirAsync(Arg.Any<ContaDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObterPorId_ComCacheMissEContaInexistente_RetornaNull_ENaoPopulaCache()
    {
        _cache.ObterAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ContaDto?)null);
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conta?)null);
        var handler = new ObterContaPorIdQueryHandler(_repositorio, _cache);

        var resultado = await handler.Handle(new ObterContaPorIdQuery(Guid.NewGuid()), default);

        resultado.Should().BeNull();
        await _cache.DidNotReceive().DefinirAsync(Arg.Any<ContaDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListarContas_RetornaResultadoPaginado()
    {
        var contas = new List<Conta> { Conta.Criar("Roberto", Cpf.Criar(CpfValido)) };
        _repositorio.ListarAsync(1, 20, Arg.Any<CancellationToken>()).Returns(contas);
        _repositorio.ContarAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new ListarContasQueryHandler(_repositorio);

        var resultado = await handler.Handle(new ListarContasQuery(1, 20), default);

        resultado.Total.Should().Be(1);
        resultado.Itens.Should().HaveCount(1);
        resultado.Pagina.Should().Be(1);
    }
}
