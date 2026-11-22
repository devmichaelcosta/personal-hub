using MediatR;

namespace PersonalHub.Application.CasosUso
{
    public class DeletarDespesaCommand : IRequest<Resposta>
    {
        public DeletarDespesaCommand(int despesaId)
        {
            DespesaId = despesaId;
        }
        public DeletarDespesaCommand()
        {

        }
        public int DespesaId { get; set; }
    }
}
