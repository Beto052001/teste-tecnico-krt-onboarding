using KRT.Onboarding.Api.Contracts;
using KRT.Onboarding.Application.Common.Models;
using KRT.Onboarding.Application.Contas.Commands;
using KRT.Onboarding.Application.Contas.Dtos;
using KRT.Onboarding.Application.Contas.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRT.Onboarding.Api.Controllers;

/// <summary>CRUD de contas de clientes. Controllers finos: apenas traduzem HTTP ⇄ casos de uso.</summary>
[ApiController]
[Route("api/contas")]
[Produces("application/json")]
public sealed class ContasController : ControllerBase
{
    private readonly ISender _mediator;

    public ContasController(ISender mediator) => _mediator = mediator;

    /// <summary>Cria uma nova conta.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] CriarContaRequest request, CancellationToken cancellationToken)
    {
        var conta = await _mediator.Send(new CriarContaCommand(request.NomeTitular, request.Cpf), cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = conta.Id }, conta);
    }

    /// <summary>Obtém uma conta por id (leitura servida por cache quando disponível).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var conta = await _mediator.Send(new ObterContaPorIdQuery(id), cancellationToken);
        return conta is null ? NotFound() : Ok(conta);
    }

    /// <summary>Lista contas de forma paginada.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _mediator.Send(new ListarContasQuery(pagina, tamanhoPagina), cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Atualiza o titular e/ou o status de uma conta.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarContaRequest request,
        CancellationToken cancellationToken)
    {
        var conta = await _mediator.Send(
            new AtualizarContaCommand(id, request.NomeTitular, request.Status),
            cancellationToken);
        return Ok(conta);
    }

    /// <summary>Remove uma conta.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoverContaCommand(id), cancellationToken);
        return NoContent();
    }
}
