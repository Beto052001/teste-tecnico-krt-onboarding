using FluentAssertions;
using KRT.Onboarding.Application.Common.Exceptions;
using KRT.Onboarding.Application.Contas.Abstractions;
using KRT.Onboarding.Application.Contas.Commands;
using KRT.Onboarding.Domain.Common;
using KRT.Onboarding.Domain.Contas;
using NSubstitute;
using Xunit;

namespace KRT.Onboarding.Application.Tests.Contas;

public class ContaCommandHandlerTests
{
    private const string CpfValido = "52998224725";

    private readonly IContaRepository _repositorio = Substitute.For<IContaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IContaCache _cache = Substitute.For<IContaCache>();

    private static Conta ContaExistente() => Conta.Criar("Titular Teste", Cpf.Criar(CpfValido));

    // ---- CriarConta ----

    [Fact]
    public async Task CriarConta_ComCpfNovo_PersisteECommita()
    {
        _repositorio.ExistePorCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CriarContaCommandHandler(_repositorio, _unitOfWork);

        var dto = await handler.Handle(new CriarContaCommand("Roberto Marquini", CpfValido), default);

        dto.NomeTitular.Should().Be("Roberto Marquini");
        dto.Status.Should().Be("Ativa");
        await _repositorio.Received(1).AdicionarAsync(Arg.Any<Conta>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CriarConta_ComCpfJaExistente_LancaConflict_ENaoPersiste()
    {
        _repositorio.ExistePorCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CriarContaCommandHandler(_repositorio, _unitOfWork);

        var acao = () => handler.Handle(new CriarContaCommand("Roberto", CpfValido), default);

        await acao.Should().ThrowAsync<ConflictException>();
        await _repositorio.DidNotReceive().AdicionarAsync(Arg.Any<Conta>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CriarConta_ComCpfInvalido_LancaDomainException()
    {
        var handler = new CriarContaCommandHandler(_repositorio, _unitOfWork);

        var acao = () => handler.Handle(new CriarContaCommand("Roberto", "12345678900"), default);

        await acao.Should().ThrowAsync<DomainException>();
    }

    // ---- AtualizarConta ----

    [Fact]
    public async Task AtualizarConta_Inexistente_LancaNotFound()
    {
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conta?)null);
        var handler = new AtualizarContaCommandHandler(_repositorio, _unitOfWork, _cache);

        var acao = () => handler.Handle(
            new AtualizarContaCommand(Guid.NewGuid(), "Novo", StatusConta.Ativa), default);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AtualizarConta_Existente_AtualizaCommitaEInvalidaCache()
    {
        var conta = ContaExistente();
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(conta);
        var handler = new AtualizarContaCommandHandler(_repositorio, _unitOfWork, _cache);

        var dto = await handler.Handle(
            new AtualizarContaCommand(conta.Id, "Novo Nome", StatusConta.Inativa), default);

        dto.NomeTitular.Should().Be("Novo Nome");
        dto.Status.Should().Be("Inativa");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoverAsync(conta.Id, Arg.Any<CancellationToken>());
    }

    // ---- RemoverConta ----

    [Fact]
    public async Task RemoverConta_Inexistente_LancaNotFound()
    {
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conta?)null);
        var handler = new RemoverContaCommandHandler(_repositorio, _unitOfWork, _cache);

        var acao = () => handler.Handle(new RemoverContaCommand(Guid.NewGuid()), default);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoverConta_Existente_RemoveCommitaEInvalidaCache()
    {
        var conta = ContaExistente();
        _repositorio.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(conta);
        var handler = new RemoverContaCommandHandler(_repositorio, _unitOfWork, _cache);

        await handler.Handle(new RemoverContaCommand(conta.Id), default);

        _repositorio.Received(1).Remover(conta);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoverAsync(conta.Id, Arg.Any<CancellationToken>());
    }
}
