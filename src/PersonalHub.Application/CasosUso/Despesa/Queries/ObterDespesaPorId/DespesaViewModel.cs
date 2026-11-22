using PersonalHub.Domain.Entidades;

namespace PersonalHub.Application.CasosUso
{
    public class DespesaViewModel
    {
        public DespesaViewModel()
        {
                
        }
        public DespesaViewModel(int id, 
                                string titulo, 
                                string descritivo,
                                string valorFormatado, 
                                int qtdParcelas,
                                List<string> tags, 
                                List<ParcelaViewModel> parcelas, 
                                DateTime vencimento)
        {
            Id = id;
            Valor = valorFormatado;
            TagsDespesas = tags;
            Titulo = titulo;
            Descritivo = descritivo;
            QtdParcelas = qtdParcelas;
            Parcelas = parcelas;
            Vencimento = vencimento.ToString("dd/MM/yyyy");
        }

        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descritivo { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public int QtdParcelas { get; set; } = 0;
        public bool Recorrente { get; set; }
        public List<string> TagsDespesas { get; set; } = new List<string>();
        public List<ParcelaViewModel> Parcelas { get; set; } = new List<ParcelaViewModel>();
        public string Vencimento { get; set; } = string.Empty;

        public static implicit operator DespesaViewModel(Despesa despesa)
        {
            var parcelasViewModel = despesa.Parcelas.Select(parcela =>
                new ParcelaViewModel(
                    parcela.Numero, 
                    parcela.Valor.ToString("N2"), 
                    parcela.Vencimento
                )
            ).ToList();

            var model = new DespesaViewModel(
                despesa.Id,
                despesa.Titulo ?? string.Empty,
                despesa.Descritivo,
                despesa.Valor.GetValueOrDefault().ToString("N2"),
                despesa.QtdParcelas ?? 0,
                despesa.TagsDespesas.Select(e => e.Tag.Nome).ToList(),
                parcelasViewModel,
                despesa.Vencimento
            );
            model.Recorrente = despesa.Recorrente;
            return model;
        }
    }
}
