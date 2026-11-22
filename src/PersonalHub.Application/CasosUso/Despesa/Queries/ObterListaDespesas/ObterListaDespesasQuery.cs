using MediatR;

namespace PersonalHub.Application.CasosUso
{
    public class ObterListaDespesasQuery : IRequest<List<DespesaItemListaDTO>>
    {
        public string TermoPesquisa { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;

        public ObterListaDespesasQuery(string usuario, string? termoPesquisa)
        {
            Usuario = usuario;  
            TermoPesquisa = termoPesquisa ?? string.Empty;
        }

        public ObterListaDespesasQuery()
        {

        }
    }
}
