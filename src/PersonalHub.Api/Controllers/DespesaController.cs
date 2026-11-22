using PersonalHub.Application;
using PersonalHub.Application.CasosUso;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace PersonalHub.Api.Controllers;

[ApiController]
//[ApiVersion("1.0"), ApiExplorerSettings(IgnoreApi = true)]
//[Route("api/v{version:apiVersion}/Despesa")]
[Route("api/v1/Despesa")]
public class DespesaController : Controller
{
    private readonly IMediator _mediator;

    public DespesaController(IMediator mediator)
    {
        this._mediator = mediator;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        Resposta resposta = await _mediator.Send(new DeletarDespesaCommand(id));
        if (resposta.Sucesso) return NoContent();
        return BadRequest();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        DespesaViewModel resposta = await _mediator.Send(new ObterDespesaPorIdQuery(id));
        return Ok(resposta);
    }

    [HttpGet("{usuario}/{termoPesquisa?}")]
    public async Task<IActionResult> Get(string usuario, string? termoPesquisa)
    {
        List<DespesaItemListaDTO> despesas = await _mediator.Send(new ObterListaDespesasQuery(usuario, termoPesquisa));
        return Ok(despesas);
    }
}
