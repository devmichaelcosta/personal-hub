using MediatR;

namespace PersonalHub.Application.CasosUso
{
    public class ObterDespesaPorIdQuery : IRequest<DespesaViewModel>
    {
        public ObterDespesaPorIdQuery(int id)
        {
            this.Id = id;
        }
        public ObterDespesaPorIdQuery()
        {
                
        }
        public int Id { get;  set; }
    }

   
}
