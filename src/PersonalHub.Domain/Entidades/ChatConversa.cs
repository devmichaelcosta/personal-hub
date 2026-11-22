namespace PersonalHub.Domain.Entidades;

public class ChatConversa : Entity
{
    protected ChatConversa()
    {
        Mensagens = new List<ChatMensagem>();
    }

    public ChatConversa(string usuarioId, DateTime criadaEm)
    {
        UsuarioId = usuarioId;
        CriadaEm = criadaEm;
        Mensagens = new List<ChatMensagem>();
    }

    public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;
    public DateTime CriadaEm { get; set; }
    public List<ChatMensagem> Mensagens { get; set; }
}
