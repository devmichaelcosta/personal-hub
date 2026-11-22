using System.Text.Json.Serialization;

namespace PersonalHub.Web.Services.Chat;

public sealed class ChatInterpretacao
{
    [JsonPropertyName("acao")]
    public string Acao { get; set; } = "esclarecimento";

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = string.Empty;

    [JsonPropertyName("consulta")]
    public ChatConsultaIntent Consulta { get; set; } = new();

    [JsonPropertyName("despesas")]
    public List<ChatDespesaExtraida> Despesas { get; set; } = new();

    [JsonPropertyName("edicao")]
    public ChatEdicaoIntent? Edicao { get; set; }
}

public sealed class ChatEdicaoIntent
{
    [JsonPropertyName("textoBusca")]
    public string? TextoBusca { get; set; }

    [JsonPropertyName("titulo")]
    public string? Titulo { get; set; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }

    [JsonPropertyName("valorParcela")]
    public decimal? ValorParcela { get; set; }

    [JsonPropertyName("vencimento")]
    public DateOnly? Vencimento { get; set; }

    [JsonPropertyName("quantidadeParcelas")]
    public int? QuantidadeParcelas { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("participantes")]
    public List<string>? Participantes { get; set; }
}

public sealed class ChatConsultaIntent
{
    [JsonPropertyName("inicio")]
    public DateOnly? Inicio { get; set; }

    [JsonPropertyName("fim")]
    public DateOnly? Fim { get; set; }

    [JsonPropertyName("inicioComparacao")]
    public DateOnly? InicioComparacao { get; set; }

    [JsonPropertyName("fimComparacao")]
    public DateOnly? FimComparacao { get; set; }

    [JsonPropertyName("texto")]
    public string? Texto { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("situacao")]
    public string Situacao { get; set; } = "todas";

    [JsonPropertyName("agruparPor")]
    public string AgruparPor { get; set; } = "nenhum";
}

public sealed class ChatDespesaExtraida
{
    private decimal? _valorParcela;

    [JsonPropertyName("titulo")]
    public string? Titulo { get; set; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }

    [JsonPropertyName("valorParcela")]
    public decimal? ValorParcela
    {
        get => _valorParcela;
        set => _valorParcela = value.HasValue
            ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    [JsonPropertyName("vencimento")]
    public DateOnly? Vencimento { get; set; }

    [JsonPropertyName("quantidadeParcelas")]
    public int? QuantidadeParcelas { get; set; } = 1;

    [JsonPropertyName("recorrente")]
    public bool Recorrente { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("participantes")]
    public List<string> Participantes { get; set; } = new();

    [JsonPropertyName("pago")]
    public bool Pago { get; set; }

    [JsonPropertyName("origem")]
    public string Origem { get; set; } = "texto";
}

public sealed class ChatRascunho
{
    public int Indice { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorParcela { get; set; }
    public DateOnly? Vencimento { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public bool Recorrente { get; set; }
    public string? Tag { get; set; }
    public List<string> Participantes { get; set; } = new();
    public bool Pago { get; set; }
    public string Origem { get; set; } = "texto";
    public List<string> Avisos { get; set; } = new();
    public bool Confirmado { get; set; }
    public int? DespesaId { get; set; }
}

public sealed class ChatEdicaoRascunho
{
    public int Indice { get; set; }
    public int DespesaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorParcela { get; set; }
    public DateOnly Vencimento { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public bool Recorrente { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Participantes { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
    public string AssinaturaOriginal { get; set; } = string.Empty;
    public bool Confirmado { get; set; }
}

public sealed class ChatConfirmarEdicao
{
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public decimal? ValorParcela { get; set; }
    public DateOnly? Vencimento { get; set; }
    public int QuantidadeParcelas { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> UsuariosIds { get; set; } = new();
}

public sealed record ChatTurn(string Role, string Content);

public sealed record ChatOpcaoUsuario(string Id, string Nome);

public sealed record ChatOpcoes(List<ChatOpcaoUsuario> Usuarios, List<string> Tags, string UsuarioAtualId);

public sealed record ChatItemConsulta(
    string Titulo,
    string Data,
    string Situacao,
    decimal ValorTotal,
    decimal ParteUsuario,
    IReadOnlyList<ChatParteUsuario> Partes,
    IReadOnlyList<string> Tags);

public sealed record ChatParteUsuario(string Nome, decimal Valor);

public static class ChatRateio
{
    public static List<ChatParteUsuario> Dividir(decimal total, IReadOnlyList<(string Id, string Nome)> participantes)
    {
        List<(string Id, string Nome)> ordenados = participantes.OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
        if (ordenados.Count == 0)
            return new();

        decimal centavosBase = decimal.Floor(total * 100m / ordenados.Count);
        int centavosRestantes = (int)(total * 100m - centavosBase * ordenados.Count);
        return ordenados.Select((participante, indice) => new ChatParteUsuario(
            participante.Nome,
            (centavosBase + (indice < centavosRestantes ? 1m : 0m)) / 100m)).ToList();
    }

    public static decimal ObterParteDoUsuario(decimal total, IReadOnlyList<(string Id, string Nome)> participantes, string usuarioId)
    {
        List<(string Id, string Nome)> ordenados = participantes.OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
        int indice = ordenados.FindIndex(item => item.Id == usuarioId);
        if (indice < 0 || ordenados.Count == 0)
            return 0m;

        decimal centavosBase = decimal.Floor(total * 100m / ordenados.Count);
        int centavosRestantes = (int)(total * 100m - centavosBase * ordenados.Count);
        return (centavosBase + (indice < centavosRestantes ? 1m : 0m)) / 100m;
    }
}

public sealed record ChatGrupoConsulta(string Nome, decimal ValorTotal, decimal ParteUsuario, int Quantidade);

public sealed record ChatPayloadConsulta(
    string Periodo,
    decimal ValorTotal,
    decimal ParteUsuario,
    int Quantidade,
    IReadOnlyList<ChatItemConsulta> Itens,
    IReadOnlyList<ChatGrupoConsulta> Grupos,
    IReadOnlyList<ChatGrupoConsulta> Situacoes,
    ChatPayloadComparacao? Comparacao = null);

public sealed record ChatPayloadComparacao(
    string PeriodoAtual,
    decimal TotalAtual,
    decimal ParteUsuarioAtual,
    string PeriodoComparado,
    decimal TotalComparado,
    decimal ParteUsuarioComparada,
    decimal Diferenca);

public sealed record ChatMensagemDto(
    int Id,
    string Papel,
    string Tipo,
    string Conteudo,
    DateTime CriadaEm,
    IReadOnlyList<ChatRascunho>? Rascunhos,
    ChatPayloadConsulta? Consulta,
    IReadOnlyList<ChatEdicaoRascunho>? Edicoes = null);

public sealed record ChatRespostaDto(
    bool Sucesso,
    string Mensagem,
    ChatMensagemDto? Resposta,
    string? Erro = null,
    IReadOnlyList<int>? IndicesComDuplicidade = null);

public sealed class ChatConfirmacaoRascunho
{
    public int Indice { get; set; }
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public decimal? ValorParcela { get; set; }
    public DateOnly? Vencimento { get; set; }
    public int QuantidadeParcelas { get; set; }
    public bool Recorrente { get; set; }
    public bool Pago { get; set; }
    public string? Tag { get; set; }
    public List<string> UsuariosIds { get; set; } = new();
}
