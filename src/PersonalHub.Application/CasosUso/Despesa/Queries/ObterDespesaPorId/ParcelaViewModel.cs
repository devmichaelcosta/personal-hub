using PersonalHub.Domain.Entidades;

namespace PersonalHub.Application.CasosUso
{
    public class ParcelaViewModel
    {
        public int Numero { get; set; }
        public string Valor { get; set; }
        public string Vencimento { get; set; }
        public ParcelaViewModel(int numero, string valor, DateTime vencimento)
        {
            this.Vencimento = vencimento.ToString("dd/MM/yyyy");
            this.Valor = valor;
            this.Numero = numero;
        }
        public static implicit operator ParcelaViewModel(Parcela parcela)
        {
            return new ParcelaViewModel(parcela.Numero, parcela.Valor.ToString("N2"), parcela.Vencimento);
        }
    }
}
