using FluentAssertions;
using KRT.Onboarding.Application.Contas.Queries;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Infrastructure.Caching;
using KRT.Onboarding.Infrastructure.Persistence.Repositories;

namespace KRT.Onboarding.IntegrationTests.Contas;

/// <summary>
/// Cache-aside de ponta a ponta com Redis real (a resposta de custo do enunciado): o miss
/// consulta o banco e popula o cache; o hit serve do cache sem tocar o banco; o write
/// invalida a chave. Provamos o hit removendo a linha do banco entre as leituras — se a
/// segunda leitura ainda retorna, foi servida do cache.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CacheAsideTests
{
    private readonly IntegrationTestFixture _fixture;

    public CacheAsideTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Leitura_PopulaNoMiss_E_ServeDoCacheNoHit_SemTocarOBanco()
    {
        var cpf = CpfFaker.Novo();
        var id = await CriarContaPersistidaAsync("Cacheada", cpf);
        var cache = _fixture.NewContaCache();

        // 1ª leitura: cache vazio (miss) -> consulta o banco e popula.
        (await cache.ObterAsync(id)).Should().BeNull("o cache começa vazio");
        var primeira = await ConsultarAsync(id, cache);
        primeira.Should().NotBeNull();
        primeira!.NomeTitular.Should().Be("Cacheada");

        // Apaga a linha do banco. Se a próxima leitura ainda devolver, veio do cache.
        await RemoverDoBancoAsync(id);

        var segunda = await ConsultarAsync(id, cache);

        segunda.Should().NotBeNull("o hit deve servir do cache sem tocar o banco");
        segunda!.Id.Should().Be(id);
    }

    [Fact]
    public async Task Invalidacao_RemoveAChave_E_ProximaLeituraRefleteOBanco()
    {
        var cpf = CpfFaker.Novo();
        var id = await CriarContaPersistidaAsync("Para Invalidar", cpf);
        var cache = _fixture.NewContaCache();

        await ConsultarAsync(id, cache);          // popula o cache
        (await cache.ObterAsync(id)).Should().NotBeNull();

        await RemoverDoBancoAsync(id);            // estado do banco mudou (linha removida)
        await cache.RemoverAsync(id);             // write invalida a chave (como faz o Update/Delete)

        (await cache.ObterAsync(id)).Should().BeNull("a chave foi invalidada");
        (await ConsultarAsync(id, cache)).Should().BeNull("agora o miss reflete o banco vazio");
    }

    private async Task<Application.Contas.Dtos.ContaDto?> ConsultarAsync(Guid id, DistributedContaCache cache)
    {
        await using var ctx = _fixture.NewDbContext();
        var handler = new ObterContaPorIdQueryHandler(new ContaRepository(ctx), cache);
        return await handler.Handle(new ObterContaPorIdQuery(id), default);
    }

    private async Task<Guid> CriarContaPersistidaAsync(string nome, string cpf)
    {
        await using var ctx = _fixture.NewDbContext();
        var conta = Conta.Criar(nome, Cpf.Criar(cpf));
        ctx.Contas.Add(conta);
        await ctx.SaveChangesAsync();
        return conta.Id;
    }

    private async Task RemoverDoBancoAsync(Guid id)
    {
        await using var ctx = _fixture.NewDbContext();
        var conta = await ctx.Contas.FindAsync(id);
        ctx.Contas.Remove(conta!);
        await ctx.SaveChangesAsync();
    }
}
