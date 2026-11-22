using PersonalHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Application.CasosUso;

public class DeletarDespesaCommandHandler : IRequestHandler<DeletarDespesaCommand, Resposta>
{
    private readonly PersonalHubContext _context;

    public DeletarDespesaCommandHandler(PersonalHubContext context)
    {
        this._context = context;
    }

    public async Task<Resposta> Handle(DeletarDespesaCommand command, CancellationToken cancellationToken)
    {
        var despesa = await _context.Despesas.SingleOrDefaultAsync(e => e.Id == command.DespesaId, cancellationToken);

        if (despesa == null) return new Resposta { Mensagem = "Despesa nao encontrada." };

        _context.Despesas.Remove(despesa);
        await _context.SaveChangesAsync(cancellationToken);
        return new Resposta { Sucesso = true };
    }
}
