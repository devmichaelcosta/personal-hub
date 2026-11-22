namespace PersonalHub.Domain.Entidades
{
    public class UsuarioDespesa : Entity
    {
        public string UsuarioId { get; set; } = string.Empty;
        public int DespesaId { get; set; }
        public Despesa Despesa { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;

        public UsuarioDespesa(string usuarioId)
        {
            this.UsuarioId = usuarioId; 
        }

        public UsuarioDespesa()
        {

        }
    }
}
