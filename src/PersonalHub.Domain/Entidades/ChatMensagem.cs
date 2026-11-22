namespace PersonalHub.Domain.Entidades;

public class ChatMensagem : Entity
{
    protected ChatMensagem()
    {
    }

    public ChatMensagem(int conversaId, string papel, string tipo, string conteudo, DateTime criadaEm, string? dadosJson = null)
    {
        ConversaId = conversaId;
        Papel = papel;
        Tipo = tipo;
        Conteudo = conteudo;
        CriadaEm = criadaEm;
        DadosJson = dadosJson;
    }

    public int ConversaId { get; set; }
    public ChatConversa Conversa { get; set; } = null!;
    public string Papel { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string? DadosJson { get; set; }
    public DateTime CriadaEm { get; set; }
}
