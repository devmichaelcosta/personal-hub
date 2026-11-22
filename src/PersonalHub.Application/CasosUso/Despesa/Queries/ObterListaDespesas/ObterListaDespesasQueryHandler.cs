using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Application.CasosUso
{
    public class ObterListaDespesasQueryHandler : IRequestHandler<ObterListaDespesasQuery, List<DespesaItemListaDTO>>
    {
        private readonly PersonalHubContext _context;

        public ObterListaDespesasQueryHandler(PersonalHubContext context)
        {
            _context = context;
        }

        public async Task<List<DespesaItemListaDTO>> Handle(ObterListaDespesasQuery request,
                                                            CancellationToken cancellationToken)
        {
            IQueryable<Despesa> queryDespesas = _context.ObterDespesasFiltradas(request.Usuario, request.TermoPesquisa);

            var despesas = await queryDespesas
                .Select(despesaItem => new DespesaItemListaDTO
                {
                    Id = despesaItem.Id,
                    Titulo = despesaItem.Titulo ?? string.Empty,
                    Descritivo = despesaItem.Descritivo,
                    QtdParcelas = despesaItem.QtdParcelas ?? 0,
                    Valor = despesaItem.Valor.GetValueOrDefault().ToString("N2"),
                    Vencimento = despesaItem.Vencimento.ToString("dd/MM/yyyy")                    
                })
                .OrderByDescending(e => e.Id)
                .ToListAsync(cancellationToken);

            return despesas;
        }
    }
}
