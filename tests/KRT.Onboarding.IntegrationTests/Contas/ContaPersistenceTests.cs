using FluentAssertions;
using KRT.Onboarding.Domain.Contas;
using KRT.Onboarding.Infrastructure.Messaging;
using KRT.Onboarding.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KRT.Onboarding.IntegrationTests.Contas;

/// <summary>
/// Persistência de ponta a ponta contra Postgres real: mapeamentos (CPF owned, enum como
/// texto), índice único de CPF, paginação e — o ponto da etapa de eventos — a gravação da
/// outbox na MESMA transação do agregado.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ContaPersistenceTests
{
    private readonly IntegrationTestFixture _fixture;

    public ContaPersistenceTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Salvar_E_LerDeVolta_PreservaCamposEMapeamentos()
    {
        var cpf = CpfFaker.Novo();
        var id = await CriarContaPersistidaAsync("Roberto Marquini", cpf);

        await using var leitura = _fixture.NewDbContext();
        var repo = new ContaRepository(leitura);
        var conta = await repo.ObterPorIdAsync(id);

        conta.Should().NotBeNull();
        conta!.NomeTitular.Should().Be("Roberto Marquini");
        conta.Cpf.Valor.Should().Be(cpf);            // armazenado limpo (só dígitos)
        conta.Status.Should().Be(StatusConta.Ativa); // enum persistido como texto e relido
    }

    [Fact]
    public async Task ExistePorCpfAsync_RefleteOQueFoiPersistido()
    {
        var cpf = CpfFaker.Novo();
        await CriarContaPersistidaAsync("Fulano", cpf);

        await using var ctx = _fixture.NewDbContext();
        var repo = new ContaRepository(ctx);

        (await repo.ExistePorCpfAsync(cpf)).Should().BeTrue();
        (await repo.ExistePorCpfAsync(CpfFaker.Novo())).Should().BeFalse();
    }

    [Fact]
    public async Task Cpf_Duplicado_EhRejeitadoPeloIndiceUnico()
    {
        var cpf = CpfFaker.Novo();
        await CriarContaPersistidaAsync("Primeiro", cpf);

        await using var ctx = _fixture.NewDbContext();
        ctx.Contas.Add(Conta.Criar("Segundo", Cpf.Criar(cpf)));

        var salvar = async () => await ctx.SaveChangesAsync();

        await salvar.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Listar_RetornaPaginadoEOrdenadoPorCriacaoDescendente()
    {
        // Criadas em sequência: a última é a mais recente (CriadaEmUtc maior).
        var primeira = await CriarContaPersistidaAsync("C1", CpfFaker.Novo());
        await Task.Delay(5);
        var segunda = await CriarContaPersistidaAsync("C2", CpfFaker.Novo());
        await Task.Delay(5);
        var terceira = await CriarContaPersistidaAsync("C3", CpfFaker.Novo());

        await using var ctx = _fixture.NewDbContext();
        var repo = new ContaRepository(ctx);

        var pagina = await repo.ListarAsync(pagina: 1, tamanhoPagina: 2);

        pagina.Should().HaveCount(2);
        pagina.Select(c => c.CriadaEmUtc).Should().BeInDescendingOrder();
        pagina[0].Id.Should().Be(terceira); // mais recente no topo
        (await repo.ContarAsync()).Should().BeGreaterThanOrEqualTo(3);

        // Garante que as três entraram (em alguma página).
        var ids = new[] { primeira, segunda, terceira };
        ids.Should().OnlyContain(idCriado => idCriado != Guid.Empty);
    }

    [Fact]
    public async Task AoCriarConta_Outbox_RecebeContaCriada_NaMesmaTransacao()
    {
        var cpf = CpfFaker.Novo();
        var id = await CriarContaPersistidaAsync("Com Evento", cpf);

        // Contexto novo: prova que a mensagem foi de fato persistida (não é estado em memória).
        // O filtro pelo id é em memória porque "conteudo" é jsonb (não aceita LIKE no Postgres).
        await using var ctx = _fixture.NewDbContext();
        var criadas = await ctx.OutboxMessages
            .Where(m => m.Tipo == "ContaCriada")
            .ToListAsync();
        var pendente = criadas.SingleOrDefault(m => m.Conteudo.Contains(id.ToString()));

        pendente.Should().NotBeNull("a conta e o evento devem ser gravados na mesma transação");
        pendente!.ProcessadoEmUtc.Should().BeNull(); // ainda não publicado pelo worker
        pendente.Conteudo.Should().Contain(cpf);
    }

    /// <summary>Cria uma conta via repositório e commita (Unit of Work = DbContext).</summary>
    private async Task<Guid> CriarContaPersistidaAsync(string nome, string cpf)
    {
        await using var ctx = _fixture.NewDbContext();
        var repo = new ContaRepository(ctx);
        var conta = Conta.Criar(nome, Cpf.Criar(cpf));

        await repo.AdicionarAsync(conta);
        await ctx.SaveChangesAsync();

        return conta.Id;
    }
}
