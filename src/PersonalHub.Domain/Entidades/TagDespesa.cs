namespace PersonalHub.Domain.Entidades
{
    public class TagDespesa
    {
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
        public int DespesaId { get; set; }
        public Despesa Despesa { get; set; } = null!;
    }
}
