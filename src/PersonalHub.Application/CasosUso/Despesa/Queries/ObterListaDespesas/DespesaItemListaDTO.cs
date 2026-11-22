namespace PersonalHub.Application.CasosUso
{
    public class DespesaItemListaDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descritivo { get; set; } = string.Empty;
        public int QtdParcelas { get; set; }
        public string Valor { get; set; } = string.Empty;
        public string Vencimento { get; set; } = string.Empty;
    }
}
