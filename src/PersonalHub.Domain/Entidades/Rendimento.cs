namespace PersonalHub.Domain.Entidades
{
    public class Rendimento:Entity
    {
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public string Descritivo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = null!;
        public string UsuarioId { get; set; } = string.Empty;

        public void Atualizar(IRendimento rendimento)
        {
            this.Data = rendimento.Data;
            this.Valor = rendimento.Valor;  
            this.Descritivo = rendimento.Descritivo;    
            this.Titulo = rendimento.Titulo;
            this.UsuarioId = rendimento.UsuarioId;
        }
    }
}
