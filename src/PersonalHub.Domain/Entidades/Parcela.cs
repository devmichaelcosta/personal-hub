namespace PersonalHub.Domain.Entidades
{
    public class Parcela: Entity
    {
        public int Numero { get; set; }
        public decimal Valor { get; set; }
        public int DespesaId { get; set; }
        public Despesa Despesa { get; set; } = null!;
        public DateTime Vencimento { get; set; }
        public bool Pago { get; private set; }
        public string ObterVencimentoFormatado() => this.Vencimento.ToString("dd/MM/yyyy");
        public Parcela(int numero, decimal valor, DateTime vencimento)
        {
            this.Numero = numero;
            this.Valor = valor;
            this.Vencimento = vencimento;
        }

        public void MarcarComoPaga()
        {
            this.Pago = true;
        }

        public void MarcarComoNaoPaga()
        {
            this.Pago = false;
        }
    }
}
