using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using PersonalHub.Web.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PersonalHub.Web.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public sealed class ChatController : Controller
{
    private const long TamanhoMaximoImagem = 5 * 1024 * 1024;
    private readonly ChatAssistenteService _chat;

    public ChatController(ChatAssistenteService chat)
    {
        _chat = chat;
    }

    [HttpGet]
    public async Task<IActionResult> Carregar(CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        return Json(await _chat.CarregarAsync(usuarioId, cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5_300_000)]
    public async Task<IActionResult> Enviar(
        [FromForm] string? mensagem,
        [FromForm] IFormFile? imagem,
        CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        byte[]? bytesImagem = null;
        string? mimeType = null;
        try
        {
            if (imagem != null)
            {
                if (imagem.Length <= 0 || imagem.Length > TamanhoMaximoImagem)
                    return BadRequest(new ChatRespostaDto(false, "A imagem deve ter até 5 MB.", null, "imagem_tamanho_invalido"));

                await using MemoryStream stream = new((int)imagem.Length);
                await imagem.CopyToAsync(stream, cancellationToken);
                bytesImagem = stream.ToArray();
                mimeType = IdentificarImagem(bytesImagem);
                if (mimeType == null)
                    return BadRequest(new ChatRespostaDto(false, "Formato não suportado. Envie uma imagem JPEG, PNG ou WebP.", null, "imagem_formato_invalido"));
            }

            ChatRespostaDto resposta = await _chat.EnviarAsync(usuarioId, mensagem, bytesImagem, mimeType, cancellationToken);
            return resposta.Sucesso ? Json(resposta) : BadRequest(resposta);
        }
        finally
        {
            if (bytesImagem != null)
                Array.Clear(bytesImagem);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(
        [FromForm] int mensagemId,
        [FromForm] int indice,
        [FromForm] string? titulo,
        [FromForm] string? descricao,
        [FromForm] string? valorParcela,
        [FromForm] string? vencimento,
        [FromForm] int quantidadeParcelas,
        [FromForm] bool recorrente,
        [FromForm] bool pago,
        [FromForm] string? tag,
        [FromForm] bool ignorarPossivelDuplicidade,
        CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        decimal? valor = TentarLerDecimal(valorParcela);
        DateOnly? data = DateOnly.TryParseExact(vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dataLida)
            ? dataLida
            : null;
        IReadOnlyList<string> usuariosIds = Request.Form["usuariosIds"]
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        ChatRespostaDto resposta = await _chat.ConfirmarDespesaAsync(
            usuarioId,
            mensagemId,
            indice,
            titulo,
            descricao,
            valor,
            data,
            quantidadeParcelas,
            recorrente,
            pago,
            tag,
            usuariosIds,
            ignorarPossivelDuplicidade,
            cancellationToken);

        return resposta.Sucesso ? Json(resposta) : BadRequest(resposta);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarLote(
        [FromForm] int mensagemId,
        [FromForm] string? rascunhosJson,
        [FromForm] bool ignorarPossiveisDuplicidades,
        CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        List<ChatConfirmacaoRascunho>? confirmacoes;
        try
        {
            confirmacoes = JsonSerializer.Deserialize<List<ChatConfirmacaoRascunho>>(
                rascunhosJson ?? string.Empty,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return BadRequest(new ChatRespostaDto(false, "Não foi possível ler os lançamentos enviados. Atualize o histórico e tente novamente.", null, "lote_invalido"));
        }

        if (confirmacoes == null)
            return BadRequest(new ChatRespostaDto(false, "Nenhum lançamento foi enviado para confirmação.", null, "lote_invalido"));

        ChatRespostaDto resposta = await _chat.ConfirmarDespesasEmLoteAsync(
            usuarioId,
            mensagemId,
            confirmacoes,
            ignorarPossiveisDuplicidades,
            cancellationToken);

        return resposta.Sucesso ? Json(resposta) : BadRequest(resposta);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEdicao(
        [FromForm] int mensagemId,
        [FromForm] int indice,
        [FromForm] string? edicaoJson,
        CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        ChatConfirmarEdicao? confirmacao;
        try
        {
            confirmacao = JsonSerializer.Deserialize<ChatConfirmarEdicao>(
                edicaoJson ?? string.Empty,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return BadRequest(new ChatRespostaDto(false, "Não foi possível ler os dados da edição. Atualize o histórico e tente novamente.", null, "edicao_invalida"));
        }

        if (confirmacao == null)
            return BadRequest(new ChatRespostaDto(false, "Nenhum dado de edição foi enviado.", null, "edicao_invalida"));

        ChatRespostaDto resposta = await _chat.ConfirmarEdicaoAsync(usuarioId, mensagemId, indice, confirmacao, cancellationToken);
        return resposta.Sucesso ? Json(resposta) : BadRequest(resposta);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Limpar(CancellationToken cancellationToken)
    {
        string? usuarioId = ObterUsuarioId();
        if (usuarioId == null)
            return Unauthorized();

        await _chat.LimparHistoricoAsync(usuarioId, cancellationToken);
        return Json(new { sucesso = true });
    }

    private string? ObterUsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static decimal? TentarLerDecimal(string? valor)
    {
        if (decimal.TryParse(valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal valorInvariant))
            return valorInvariant;
        if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out decimal valorPtBr))
            return valorPtBr;
        return null;
    }

    private static string? IdentificarImagem(byte[] conteudo)
    {
        if (conteudo.Length >= 3 && conteudo[0] == 0xFF && conteudo[1] == 0xD8 && conteudo[2] == 0xFF)
            return "image/jpeg";
        if (conteudo.Length >= 8 && conteudo.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";
        if (conteudo.Length >= 12 &&
            conteudo.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            conteudo.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
