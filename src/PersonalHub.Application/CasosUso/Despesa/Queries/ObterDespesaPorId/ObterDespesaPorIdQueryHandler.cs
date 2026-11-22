using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Application.CasosUso
{
    public class ObterDespesaPorIdQueryHandler : IRequestHandler<ObterDespesaPorIdQuery, DespesaViewModel>
    {
        private readonly PersonalHubContext _context;

        public ObterDespesaPorIdQueryHandler(PersonalHubContext context)
        {
            this._context = context;
        }

        public async Task<DespesaViewModel> Handle(ObterDespesaPorIdQuery request, 
                                                   CancellationToken cancellationToken)
        {
            Despesa despesa = await _context.Despesas.AsNoTracking().SingleAsync(m => m.Id == request.Id);

            DespesaViewModel model = despesa;
            return model;
        }
    }
}