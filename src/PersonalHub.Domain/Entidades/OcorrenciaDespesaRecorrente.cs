namespace PersonalHub.Domain.Entidades
{
    public class OcorrenciaDespesaRecorrente : Entity
    {
        protected OcorrenciaDespesaRecorrente()
        {
        }

        public int DespesaId { get; private set; }
        public Despesa Despesa { get; private set; } = null!;
        public DateTime MesReferencia { get; private set; }
        public DateTime Vencimento { get; private set; }
        public decimal Valor { get; private set; }
        public bool Pago { get; private set; }

        public OcorrenciaDespesaRecorrente(int despesaId, DateTime mesReferencia, DateTime vencimento, decimal valor)
        {
            DespesaId = despesaId;
            MesReferencia = new DateTime(mesReferencia.Year, mesReferencia.Month, 1);
            Vencimento = vencimento.Date;
            Valor = valor;
        }

        public void MarcarComoPaga() => Pago = true;

        public void MarcarComoNaoPaga() => Pago = false;
    }
}
