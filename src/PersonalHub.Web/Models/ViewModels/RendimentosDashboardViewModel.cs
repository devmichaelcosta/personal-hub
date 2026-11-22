namespace PersonalHub.Web.Models.ViewModels
{
    public class RendimentosDashboardViewModel
    {
        public RendimentosDashboardViewModel(DateTime? mesReferencia, List<RendimentoDashboardItemViewModel> rendimentos)
        {
            MesReferencia = mesReferencia;
            Rendimentos = rendimentos;
            Total = rendimentos.Sum(e => e.Valor);
        }

        public DateTime? MesReferencia { get; }
        public List<RendimentoDashboardItemViewModel> Rendimentos { get; }
        public decimal Total { get; }
        public string Periodo => MesReferencia?.ToString("MMMM yyyy") ?? "Todos os meses";
    }

    public class RendimentoDashboardItemViewModel
    {
        public RendimentoDashboardItemViewModel(int id, string titulo, string descritivo, DateTime data, decimal valor, string usuario)
        {
            Id = id;
            Titulo = titulo;
            Descritivo = descritivo;
            Data = data;
            Valor = valor;
            Usuario = usuario;
        }

        public int Id { get; }
        public string Titulo { get; }
        public string Descritivo { get; }
        public DateTime Data { get; }
        public decimal Valor { get; }
        public string Usuario { get; }
        public string ValorFormatado => Valor.ToString("C2");
    }
}
