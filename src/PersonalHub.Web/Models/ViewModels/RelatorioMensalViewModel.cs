namespace PersonalHub.Web.Models.ViewModels
{
    public class RelatorioMensalViewModel
    {
        public RelatorioMensalViewModel(DateTime dataReferencia, List<RelatorioMensalItemViewModel> meses, List<RelatorioTagViewModel> tags)
        {
            DataReferencia = dataReferencia;
            Meses = meses;
            Tags = tags;
        }

        public DateTime DataReferencia { get; }
        public List<RelatorioMensalItemViewModel> Meses { get; }
        public List<RelatorioTagViewModel> Tags { get; }
        public RelatorioMensalItemViewModel? MesAtual => Meses.LastOrDefault();
        public RelatorioMensalItemViewModel? MesAnterior => Meses.Count > 1 ? Meses[^2] : null;
        public decimal VariacaoDespesas => (MesAtual?.Despesas ?? 0) - (MesAnterior?.Despesas ?? 0);
        public decimal VariacaoRendimentos => (MesAtual?.Rendimentos ?? 0) - (MesAnterior?.Rendimentos ?? 0);
        public decimal VariacaoSaldo => (MesAtual?.Saldo ?? 0) - (MesAnterior?.Saldo ?? 0);
    }

    public class RelatorioMensalItemViewModel
    {
        public RelatorioMensalItemViewModel(DateTime mes, decimal despesas, decimal rendimentos)
        {
            Mes = mes;
            Despesas = despesas;
            Rendimentos = rendimentos;
        }

        public DateTime Mes { get; }
        public decimal Despesas { get; }
        public decimal Rendimentos { get; }
        public decimal Saldo => Rendimentos - Despesas;
    }

    public class RelatorioTagViewModel
    {
        public RelatorioTagViewModel(string tag, decimal total)
        {
            Tag = tag;
            Total = total;
        }

        public string Tag { get; }
        public decimal Total { get; }
    }
}
