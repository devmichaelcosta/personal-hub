using Microsoft.AspNetCore.Mvc.Rendering;

namespace PersonalHub.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        protected DashboardViewModel() { }

        public DashboardViewModel(decimal rendimentosDoMes,
                                  DateTime? mesReferencia,
                                  List<DespesaDashboard> despesas,
                                  string pesquisa,
                                  List<SelectListItem> usuariosDisponiveis,
                                  List<string> usuariosSelecionados,
                                  List<SelectListItem> tagsDisponiveis,
                                  List<int> tagsSelecionadas)
        {
            RendimentosDoMes = rendimentosDoMes;
            GastosDoMes = despesas.Sum(e => e.Valor);
            MesReferencia = mesReferencia;
            DataInicial = mesReferencia.HasValue
                ? new DateTime(mesReferencia.Value.Year, mesReferencia.Value.Month, 1)
                : null;
            DataFinal = DataInicial?.AddMonths(1).AddDays(-1);
            Despesas = despesas;
            Pesquisa = pesquisa;
            UsuariosDisponiveis = usuariosDisponiveis;
            UsuariosSelecionados = usuariosSelecionados;
            TagsDisponiveis = tagsDisponiveis;
            TagsSelecionadas = tagsSelecionadas;

            Total = ObterTotal();
            Rendimentos = ObterRendimentos();
            TotalDividaConjunta = ObterTotalDividaConjunta();
            Saldo = ObterSaldo();
            TotalPendente = ObterTotalPendente();
            TotalPago = ObterTotalPago();
            TotalVencido = ObterTotalVencido();
            TotalAVencer = ObterTotalAVencer();
        }

        public string? Pesquisa { get; private set; } = string.Empty;
        public decimal RendimentosDoMes { get; private set; } = 0;
        public decimal GastosDoMes { get; private set; } = 0;
        public DateTime? DataInicial { get; private set; }
        public DateTime? DataFinal { get; private set; }
        public DateTime? MesReferencia { get; private set; }
        public List<SelectListItem> UsuariosDisponiveis { get; private set; } = new List<SelectListItem>();
        public List<string> UsuariosSelecionados { get; private set; } = new List<string>();
        public List<SelectListItem> TagsDisponiveis { get; private set; } = new List<SelectListItem>();
        public List<int> TagsSelecionadas { get; private set; } = new List<int>();
        public List<DespesaDashboard> Despesas { get; private set; } = new List<DespesaDashboard>();
        public string Total { get; private set; } = string.Empty;
        public string Rendimentos { get; private set; } = string.Empty;
        public string TotalDividaConjunta { get; private set; } = string.Empty;
        public string Saldo { get; private set; } = string.Empty;
        public bool SaldoNegativo => RendimentosDoMes - GastosDoMes < 0;
        public string TotalPendente { get; private set; } = string.Empty;
        public string TotalPago { get; private set; } = string.Empty;
        public string TotalVencido { get; private set; } = string.Empty;
        public string TotalAVencer { get; private set; } = string.Empty;

        private string ObterSaldo() => FormatarValor(RendimentosDoMes - GastosDoMes);
        private string ObterTotal() => FormatarValor(Despesas.Sum(despesaAtual => despesaAtual.Valor));
        private string ObterRendimentos() => FormatarValor(RendimentosDoMes);
        private string ObterTotalPendente() => FormatarValor(Despesas.Where(e => !e.Pago).Sum(e => e.Valor));
        private string ObterTotalPago() => FormatarValor(Despesas.Where(e => e.Pago).Sum(e => e.Valor));
        private string ObterTotalVencido() => FormatarValor(Despesas.Where(e => !e.Pago && e.Venceu).Sum(e => e.Valor));
        private string ObterTotalAVencer() => FormatarValor(Despesas.Where(e => !e.Pago && e.AVencer).Sum(e => e.Valor));
        private string ObterTotalDividaConjunta() => FormatarValor(Despesas
            .Where(e => e.AvatarUsuariosEnvolvidos.Count > 1)
            .Sum(despesaAtual => despesaAtual.Valor));
        private static string FormatarValor(decimal valor) => "R$ " + valor.ToString("N2");
    }
}
