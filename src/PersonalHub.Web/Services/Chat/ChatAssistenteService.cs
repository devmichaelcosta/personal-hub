using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PersonalHub.Domain.Entidades;
using PersonalHub.Domain.Enums;
using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Services.Chat;

public sealed class ChatAssistenteService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PersonalHubContext _context;
    private readonly IChatOllamaClient _ollama;

    public ChatAssistenteService(PersonalHubContext context, IChatOllamaClient ollama)
    {
        _context = context;
        _ollama = ollama;
    }

    public async Task<ChatCarregamentoDto> CarregarAsync(string usuarioId, CancellationToken cancellationToken)
    {
        ChatConversa conversa = await ObterConversaAsync(usuarioId, cancellationToken);
        List<ChatMensagem> mensagens = await _context.ChatMensagens
            .Where(mensagem => mensagem.ConversaId == conversa.Id)
            .OrderBy(mensagem => mensagem.CriadaEm)
            .ThenBy(mensagem => mensagem.Id)
            .ToListAsync(cancellationToken);

        ChatOpcoes opcoes = await ObterOpcoesAsync(usuarioId, cancellationToken);
        return new ChatCarregamentoDto(mensagens.Select(MapearMensagem).ToList(), opcoes);
    }

    public async Task<ChatRespostaDto> EnviarAsync(
        string usuarioId,
        string? mensagem,
        byte[]? imagem,
        string? mimeType,
        CancellationToken cancellationToken)
    {
        string texto = (mensagem ?? string.Empty).Trim();
        if (texto.Length == 0 && imagem is not { Length: > 0 })
            return new ChatRespostaDto(false, "Escreva uma mensagem ou anexe uma imagem.", null, "mensagem_vazia");
        if (texto.Length > 4000)
            return new ChatRespostaDto(false, "A mensagem deve ter no máximo 4.000 caracteres.", null, "mensagem_longa");

        ChatConversa conversa = await ObterConversaAsync(usuarioId, cancellationToken);
        List<ChatMensagem> mensagensAnteriores = await _context.ChatMensagens
            .Where(item => item.ConversaId == conversa.Id)
            .OrderByDescending(item => item.CriadaEm)
            .ThenByDescending(item => item.Id)
            .Take(12)
            .ToListAsync(cancellationToken);
        mensagensAnteriores.Reverse();

        DateTime agora = DateTime.Now;
        string textoParaModelo = texto.Length > 0 ? texto : "Leia a imagem anexada e prepare rascunhos de despesas para revisão.";
        string textoHistorico = texto.Length > 0 ? texto : "[Imagem anexada para extração de despesas]";
        if (imagem is { Length: > 0 })
            textoHistorico += "\n[Imagem processada localmente; o arquivo original não foi armazenado.]";

        ChatMensagem mensagemUsuario = new(conversa.Id, "user", "texto", textoHistorico, agora);
        _context.ChatMensagens.Add(mensagemUsuario);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            ChatOpcoes opcoes = await ObterOpcoesAsync(usuarioId, cancellationToken);
            if (imagem is not { Length: > 0 } && ChatEdicaoParser.TentarInterpretar(texto, out ChatEdicaoIntent edicaoDireta))
                return await ProcessarEdicaoAsync(conversa, edicaoDireta, opcoes, cancellationToken);
            if (imagem is not { Length: > 0 } && ChatConsultaParser.TentarInterpretarTotalMensal(texto, out ChatConsultaIntent consultaDireta))
                return await ProcessarConsultaAsync(conversa, consultaDireta, cancellationToken);

            ChatInterpretacao interpretacao = await _ollama.InterpretarAsync(
                textoParaModelo,
                mensagensAnteriores.Select(item => new ChatTurn(item.Papel, item.Conteudo)).ToList(),
                opcoes.Tags,
                opcoes.Usuarios.Select(item => item.Nome).ToList(),
                imagem,
                mimeType,
                cancellationToken);

            return interpretacao.Acao switch
            {
                "consulta" => await ProcessarConsultaAsync(conversa, interpretacao.Consulta, cancellationToken),
                "rascunho" => await ProcessarRascunhosAsync(conversa, interpretacao, opcoes, cancellationToken),
                "editar" => await ProcessarEdicaoAsync(conversa, interpretacao.Edicao, opcoes, cancellationToken),
                "conversa" or "esclarecimento" => await SalvarRespostaAsync(
                    conversa,
                    "texto",
                    ObterRespostaConversacional(interpretacao),
                    null,
                    cancellationToken),
                _ => await SalvarRespostaAsync(
                    conversa,
                    "texto",
                    "Não consegui identificar se você quer consultar despesas ou preparar um lançamento. Pode explicar de outra forma?",
                    null,
                    cancellationToken)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException or InvalidDataException or JsonException)
        {
            return await SalvarRespostaAsync(
                conversa,
                "erro",
                "Não consegui acessar ou interpretar o Ollama local. Confira se ele está em execução e tente novamente. Sua mensagem foi mantida no histórico; nenhum lançamento foi gravado.",
                null,
                cancellationToken);
        }
    }

    public async Task<ChatRespostaDto> ConfirmarDespesaAsync(
        string usuarioId,
        int mensagemId,
        int indice,
        string? titulo,
        string? descricao,
        decimal? valorParcela,
        DateOnly? vencimento,
        int quantidadeParcelas,
        bool recorrente,
        bool pago,
        string? tagNome,
        IReadOnlyList<string> usuariosIds,
        bool ignorarPossivelDuplicidade,
        CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        ChatConversa? conversa = await _context.ChatConversas
            .SingleOrDefaultAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        ChatMensagem? mensagemRascunho = conversa == null
            ? null
            : await _context.ChatMensagens
                .FromSqlInterpolated($"SELECT * FROM ChatMensagens WITH (UPDLOCK, HOLDLOCK) WHERE Id = {mensagemId}")
                .SingleOrDefaultAsync(
                item => item.ConversaId == conversa.Id && item.Papel == "assistant" && item.Tipo == "rascunho",
                cancellationToken);

        if (mensagemRascunho?.DadosJson == null)
            return new ChatRespostaDto(false, "Este rascunho não está disponível para sua conta.", null, "rascunho_nao_encontrado");

        List<ChatRascunho> rascunhos = JsonSerializer.Deserialize<List<ChatRascunho>>(mensagemRascunho.DadosJson, JsonOptions) ?? new();
        ChatRascunho? rascunho = rascunhos.SingleOrDefault(item => item.Indice == indice);
        if (rascunho == null)
            return new ChatRespostaDto(false, "O lançamento selecionado não existe mais.", null, "rascunho_nao_encontrado");
        if (rascunho.Confirmado)
            return new ChatRespostaDto(false, "Este lançamento já foi confirmado.", null, "rascunho_ja_confirmado");

        string tituloNormalizado = (titulo ?? string.Empty).Trim();
        string descricaoNormalizada = (descricao ?? string.Empty).Trim();
        if (tituloNormalizado.Length is < 1 or > 100)
            return new ChatRespostaDto(false, "Informe um título com até 100 caracteres.", null, "titulo_invalido");
        if (!valorParcela.HasValue || valorParcela.Value <= 0 || valorParcela.Value > 99999999999999.99m)
            return new ChatRespostaDto(false, "Informe um valor de parcela maior que zero.", null, "valor_invalido");
        if (!vencimento.HasValue)
            return new ChatRespostaDto(false, "Informe a data de vencimento ou da compra.", null, "data_invalida");
        if (descricaoNormalizada.Length > 500)
            return new ChatRespostaDto(false, "A descrição deve ter até 500 caracteres.", null, "descricao_invalida");
        if (!recorrente && (quantidadeParcelas is < 1 or > 120))
            return new ChatRespostaDto(false, "A quantidade de parcelas deve ficar entre 1 e 120.", null, "parcelas_invalidas");

        List<string> participantes = usuariosIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (participantes.Count == 0)
            return new ChatRespostaDto(false, "Selecione ao menos um participante.", null, "participantes_invalidos");

        int usuariosValidos = await _context.Users.CountAsync(usuario => participantes.Contains(usuario.Id), cancellationToken);
        if (usuariosValidos != participantes.Count)
            return new ChatRespostaDto(false, "Um dos participantes selecionados não está disponível.", null, "participantes_invalidos");

        List<Tag> tags = new();
        if (!string.IsNullOrWhiteSpace(tagNome))
        {
            Tag? tag = await _context.Tags.SingleOrDefaultAsync(item => item.Nome == tagNome.Trim(), cancellationToken);
            if (tag == null)
                return new ChatRespostaDto(false, "Selecione uma tag existente ou deixe o campo sem tag.", null, "tag_invalida");
            tags.Add(tag);
        }

        DateTime data = vencimento.Value.ToDateTime(TimeOnly.MinValue);
        decimal quantidade = recorrente ? 1 : quantidadeParcelas;
        decimal valorTotal = decimal.Round(valorParcela.Value * quantidade, 2, MidpointRounding.AwayFromZero);
        ChatChaveDuplicidade chaveDuplicidade = new(data, valorParcela.Value, valorTotal, participantes);
        await BloquearVerificacaoDuplicidadeAsync(new[] { chaveDuplicidade }, cancellationToken);
        bool possivelDuplicidade = await ExistePossivelDuplicidadeAsync(chaveDuplicidade, cancellationToken);

        if (possivelDuplicidade && !ignorarPossivelDuplicidade)
            return new ChatRespostaDto(false, "Já existe uma despesa com o mesmo valor, data e algum participante. Revise se ela já foi lançada antes de confirmar novamente.", null, "possivel_duplicidade");

        CadastroDespesaViewModel cadastro = new()
        {
            Titulo = tituloNormalizado,
            Descritivo = descricaoNormalizada,
            QtdParcelas = recorrente ? 1 : quantidadeParcelas,
            ValorParcela = valorParcela,
            Vencimento = data,
            UsuariosEnvolvidos = participantes,
            Tipo = recorrente ? TipoCadastroDespesa.RecorrenteMensal : TipoCadastroDespesa.Parcelado
        };
        cadastro.AdicionarTags(tags);

        Despesa despesa = recorrente
            ? Despesa.CriarRecorrente(cadastro)
            : Despesa.Criar(cadastro);
        _context.Despesas.Add(despesa);
        await _context.SaveChangesAsync(cancellationToken);

        if (pago)
        {
            if (recorrente)
            {
                DateTime mesReferencia = new(data.Year, data.Month, 1);
                OcorrenciaDespesaRecorrente ocorrencia = await _context.OcorrenciasDespesasRecorrentes
                    .SingleOrDefaultAsync(item => item.DespesaId == despesa.Id && item.MesReferencia == mesReferencia, cancellationToken)
                    ?? new OcorrenciaDespesaRecorrente(despesa.Id, mesReferencia, data, valorParcela.Value);
                ocorrencia.MarcarComoPaga();
                if (_context.Entry(ocorrencia).State == EntityState.Detached)
                    _context.OcorrenciasDespesasRecorrentes.Add(ocorrencia);
            }
            else
            {
                despesa.Parcelas.OrderBy(item => item.Numero).First().MarcarComoPaga();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        rascunho.Titulo = tituloNormalizado;
        rascunho.Descricao = descricaoNormalizada;
        rascunho.ValorParcela = valorParcela.Value;
        rascunho.Vencimento = vencimento.Value;
        rascunho.QuantidadeParcelas = recorrente ? 1 : quantidadeParcelas;
        rascunho.Recorrente = recorrente;
        rascunho.Pago = pago;
        rascunho.Tag = tagNome?.Trim();
        rascunho.Participantes = participantes;
        rascunho.Confirmado = true;
        rascunho.DespesaId = despesa.Id;
        mensagemRascunho.DadosJson = JsonSerializer.Serialize(rascunhos, JsonOptions);
        mensagemRascunho.Conteudo += $"\n\nDespesa “{tituloNormalizado}” cadastrada com sucesso.";
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return new ChatRespostaDto(true, "Despesa cadastrada com sucesso.", MapearMensagem(mensagemRascunho));
    }

    public async Task<ChatRespostaDto> ConfirmarDespesasEmLoteAsync(
        string usuarioId,
        int mensagemId,
        IReadOnlyList<ChatConfirmacaoRascunho> confirmacoes,
        bool ignorarPossiveisDuplicidades,
        CancellationToken cancellationToken)
    {
        if (confirmacoes.Count is < 2 or > 20)
            return new ChatRespostaDto(false, "A confirmação em lote exige de 2 a 20 despesas pendentes.", null, "lote_invalido");

        if (confirmacoes.Select(item => item.Indice).Distinct().Count() != confirmacoes.Count)
            return new ChatRespostaDto(false, "O lote contém lançamentos repetidos. Atualize o histórico e tente novamente.", null, "lote_invalido");

        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        ChatConversa? conversa = await _context.ChatConversas
            .SingleOrDefaultAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        ChatMensagem? mensagemRascunho = conversa == null
            ? null
            : await _context.ChatMensagens
                .FromSqlInterpolated($"SELECT * FROM ChatMensagens WITH (UPDLOCK, HOLDLOCK) WHERE Id = {mensagemId}")
                .SingleOrDefaultAsync(
                    item => item.ConversaId == conversa.Id && item.Papel == "assistant" && item.Tipo == "rascunho",
                    cancellationToken);

        if (mensagemRascunho?.DadosJson == null)
            return new ChatRespostaDto(false, "Este rascunho não está disponível para sua conta.", null, "rascunho_nao_encontrado");

        List<ChatRascunho> rascunhos = JsonSerializer.Deserialize<List<ChatRascunho>>(mensagemRascunho.DadosJson, JsonOptions) ?? new();
        List<ChatRascunho> pendentes = rascunhos.Where(item => !item.Confirmado).ToList();
        if (pendentes.Count != confirmacoes.Count || !pendentes.Select(item => item.Indice).Order().SequenceEqual(confirmacoes.Select(item => item.Indice).Order()))
            return new ChatRespostaDto(false, "A lista de lançamentos pendentes mudou. Atualize o histórico antes de confirmar o lote.", null, "lote_desatualizado");

        List<(ChatRascunho Rascunho, ChatConfirmacaoRascunho Entrada, CadastroDespesaViewModel Cadastro, decimal ValorTotal, DateTime Data)> preparados = new();
        List<string> erros = new();
        foreach (ChatConfirmacaoRascunho entrada in confirmacoes)
        {
            ChatRascunho? rascunho = pendentes.SingleOrDefault(item => item.Indice == entrada.Indice);
            if (rascunho == null)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: não encontrado.");
                continue;
            }

            string titulo = (entrada.Titulo ?? string.Empty).Trim();
            string descricao = (entrada.Descricao ?? string.Empty).Trim();
            int quantidadeParcelas = entrada.Recorrente ? 1 : entrada.QuantidadeParcelas;
            if (titulo.Length is < 1 or > 100)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: informe um título com até 100 caracteres.");
                continue;
            }
            if (!entrada.ValorParcela.HasValue || entrada.ValorParcela.Value <= 0 || entrada.ValorParcela.Value > 99999999999999.99m)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: informe um valor de parcela maior que zero.");
                continue;
            }
            if (!entrada.Vencimento.HasValue || entrada.Vencimento.Value == DateOnly.MaxValue)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: informe uma data de vencimento válida.");
                continue;
            }
            if (descricao.Length > 500)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: a descrição deve ter até 500 caracteres.");
                continue;
            }
            if (!entrada.Recorrente && quantidadeParcelas is < 1 or > 120)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: a quantidade de parcelas deve ficar entre 1 e 120.");
                continue;
            }

            List<string> participantes = entrada.UsuariosIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (participantes.Count == 0 || await _context.Users.CountAsync(usuario => participantes.Contains(usuario.Id), cancellationToken) != participantes.Count)
            {
                erros.Add($"Lançamento {entrada.Indice + 1}: selecione participantes válidos.");
                continue;
            }

            List<Tag> tags = new();
            if (!string.IsNullOrWhiteSpace(entrada.Tag))
            {
                Tag? tag = await _context.Tags.SingleOrDefaultAsync(item => item.Nome == entrada.Tag.Trim(), cancellationToken);
                if (tag == null)
                {
                    erros.Add($"Lançamento {entrada.Indice + 1}: selecione uma tag existente ou deixe sem tag.");
                    continue;
                }
                tags.Add(tag);
            }

            DateTime data = entrada.Vencimento.Value.ToDateTime(TimeOnly.MinValue);
            decimal valorTotal = decimal.Round(entrada.ValorParcela.Value * quantidadeParcelas, 2, MidpointRounding.AwayFromZero);
            CadastroDespesaViewModel cadastro = new()
            {
                Titulo = titulo,
                Descritivo = descricao,
                QtdParcelas = quantidadeParcelas,
                ValorParcela = entrada.ValorParcela.Value,
                Vencimento = data,
                UsuariosEnvolvidos = participantes,
                Tipo = entrada.Recorrente ? TipoCadastroDespesa.RecorrenteMensal : TipoCadastroDespesa.Parcelado
            };
            cadastro.AdicionarTags(tags);
            preparados.Add((rascunho, entrada, cadastro, valorTotal, data));
        }

        if (erros.Count > 0)
            return new ChatRespostaDto(false, string.Join(" ", erros), null, "lote_invalido");

        await BloquearVerificacaoDuplicidadeAsync(preparados.Select(item => new ChatChaveDuplicidade(
            item.Data,
            item.Entrada.ValorParcela!.Value,
            item.ValorTotal,
            item.Cadastro.UsuariosEnvolvidos)), cancellationToken);

        List<int> indicesDuplicados = new();
        foreach (var item in preparados)
        {
            ChatChaveDuplicidade chave = new(
                item.Data,
                item.Entrada.ValorParcela!.Value,
                item.ValorTotal,
                item.Cadastro.UsuariosEnvolvidos);
            bool duplicadaNoBanco = await ExistePossivelDuplicidadeAsync(chave, cancellationToken);
            if (duplicadaNoBanco)
                indicesDuplicados.Add(item.Rascunho.Indice);
        }

        for (int i = 0; i < preparados.Count; i++)
        {
            for (int j = i + 1; j < preparados.Count; j++)
            {
                var primeiro = preparados[i];
                var segundo = preparados[j];
                bool mesmoDia = primeiro.Data.Date == segundo.Data.Date;
                bool mesmoValor = new[] { primeiro.ValorTotal, primeiro.Entrada.ValorParcela!.Value }
                    .Intersect(new[] { segundo.ValorTotal, segundo.Entrada.ValorParcela!.Value })
                    .Any();
                bool mesmoParticipante = primeiro.Cadastro.UsuariosEnvolvidos
                    .Intersect(segundo.Cadastro.UsuariosEnvolvidos, StringComparer.Ordinal).Any();
                if (mesmoDia && mesmoValor && mesmoParticipante)
                {
                    indicesDuplicados.Add(primeiro.Rascunho.Indice);
                    indicesDuplicados.Add(segundo.Rascunho.Indice);
                }
            }
        }

        indicesDuplicados = indicesDuplicados.Distinct().Order().ToList();
        if (indicesDuplicados.Count > 0 && !ignorarPossiveisDuplicidades)
            return new ChatRespostaDto(false, "Um ou mais lançamentos têm o mesmo valor, data e participante de uma despesa existente ou de outro item deste lote. Revise os itens antes de continuar.", null, "possivel_duplicidade_lote", indicesDuplicados);

        List<(Despesa Despesa, (ChatRascunho Rascunho, ChatConfirmacaoRascunho Entrada, CadastroDespesaViewModel Cadastro, decimal ValorTotal, DateTime Data) Item)> criadas = new();
        foreach (var item in preparados)
        {
            Despesa despesa = item.Entrada.Recorrente
                ? Despesa.CriarRecorrente(item.Cadastro)
                : Despesa.Criar(item.Cadastro);
            _context.Despesas.Add(despesa);
            criadas.Add((despesa, item));
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (despesa, item) in criadas)
        {
            item.Rascunho.Titulo = item.Cadastro.Titulo!;
            item.Rascunho.Descricao = item.Cadastro.Descritivo ?? string.Empty;
            item.Rascunho.ValorParcela = item.Cadastro.ValorParcela!.Value;
            item.Rascunho.Vencimento = item.Entrada.Vencimento;
            item.Rascunho.QuantidadeParcelas = item.Cadastro.QtdParcelas!.Value;
            item.Rascunho.Recorrente = item.Entrada.Recorrente;
            item.Rascunho.Pago = item.Entrada.Pago;
            item.Rascunho.Tag = item.Entrada.Tag?.Trim();
            item.Rascunho.Participantes = item.Cadastro.UsuariosEnvolvidos.ToList();
            item.Rascunho.Confirmado = true;
            item.Rascunho.DespesaId = despesa.Id;

            if (item.Entrada.Pago)
            {
                if (item.Entrada.Recorrente)
                {
                    DateTime mesReferencia = new(item.Data.Year, item.Data.Month, 1);
                    OcorrenciaDespesaRecorrente ocorrencia = await _context.OcorrenciasDespesasRecorrentes
                        .SingleOrDefaultAsync(itemAtual => itemAtual.DespesaId == despesa.Id && itemAtual.MesReferencia == mesReferencia, cancellationToken)
                        ?? new OcorrenciaDespesaRecorrente(despesa.Id, mesReferencia, item.Data, item.Entrada.ValorParcela!.Value);
                    ocorrencia.MarcarComoPaga();
                    if (_context.Entry(ocorrencia).State == EntityState.Detached)
                        _context.OcorrenciasDespesasRecorrentes.Add(ocorrencia);
                }
                else
                {
                    despesa.Parcelas.OrderBy(parcela => parcela.Numero).First().MarcarComoPaga();
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        mensagemRascunho.DadosJson = JsonSerializer.Serialize(rascunhos, JsonOptions);
        mensagemRascunho.Conteudo += $"\n\n{preparados.Count} despesas cadastradas com sucesso.";
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return new ChatRespostaDto(true, $"{preparados.Count} despesas cadastradas com sucesso.", MapearMensagem(mensagemRascunho));
    }

    public async Task LimparHistoricoAsync(string usuarioId, CancellationToken cancellationToken)
    {
        ChatConversa? conversa = await _context.ChatConversas.SingleOrDefaultAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        if (conversa == null)
            return;

        await _context.ChatMensagens.Where(item => item.ConversaId == conversa.Id).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<ChatRespostaDto> ProcessarRascunhosAsync(
        ChatConversa conversa,
        ChatInterpretacao interpretacao,
        ChatOpcoes opcoes,
        CancellationToken cancellationToken)
    {
        List<ChatRascunho> rascunhos = interpretacao.Despesas.Select((item, indice) =>
        {
            List<string> participantes = new() { opcoes.UsuarioAtualId };
            List<string> avisos = new();
            foreach (string nome in item.Participantes ?? new List<string>())
            {
                string nomeInformado = (nome ?? string.Empty).Trim();
                List<ChatOpcaoUsuario> correspondencias = opcoes.Usuarios
                    .Where(usuario => string.Equals(usuario.Nome, nomeInformado, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (correspondencias.Count == 1)
                    participantes.Add(correspondencias[0].Id);
                else if (nomeInformado.Length > 0)
                    avisos.Add(correspondencias.Count == 0
                        ? $"Não reconheci o participante \"{nomeInformado}\". Confira os envolvidos antes de confirmar."
                        : $"O nome \"{nomeInformado}\" corresponde a mais de uma pessoa. Selecione os envolvidos corretos antes de confirmar.");
            }

            string titulo = (item.Titulo ?? string.Empty).Trim();
            string descricao = (item.Descricao ?? string.Empty).Trim();
            if (titulo.Length == 0)
                avisos.Add("Não identifiquei um título textual para esta transação. Informe o estabelecimento ou o nome do lançamento antes de confirmar.");
            string? tag = opcoes.Tags.FirstOrDefault(tagDisponivel => string.Equals(tagDisponivel, item.Tag?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(item.Tag) && tag == null)
                avisos.Add($"Não reconheci a tag \"{item.Tag.Trim()}\". Escolha uma tag existente ou deixe sem tag.");

            return new ChatRascunho
            {
                Indice = indice,
                Titulo = titulo[..Math.Min(titulo.Length, 100)],
                Descricao = descricao[..Math.Min(descricao.Length, 500)],
                ValorParcela = item.ValorParcela.GetValueOrDefault(),
                Vencimento = item.Vencimento,
                QuantidadeParcelas = item.QuantidadeParcelas.GetValueOrDefault(1),
                Recorrente = item.Recorrente,
                Tag = tag,
                Participantes = participantes.Distinct(StringComparer.Ordinal).ToList(),
                Pago = item.Pago,
                Origem = string.IsNullOrWhiteSpace(item.Origem) ? "texto" : item.Origem,
                Avisos = avisos
            };
        }).ToList();

        if (rascunhos.Count == 0)
            return await SalvarRespostaAsync(conversa, "texto", "Não encontrei lançamentos com dados suficientes nessa mensagem ou imagem. Confira se a imagem está legível ou descreva a despesa com valor e data.", null, cancellationToken);

        string texto = "Preparei os lançamentos abaixo. Revise os dados e confirme cada despesa para gravá-la.";

        return await SalvarRespostaAsync(conversa, "rascunho", texto, JsonSerializer.Serialize(rascunhos, JsonOptions), cancellationToken);
    }

    private async Task<ChatRespostaDto> ProcessarEdicaoAsync(
        ChatConversa conversa,
        ChatEdicaoIntent? intent,
        ChatOpcoes opcoes,
        CancellationToken cancellationToken)
    {
        string busca = (intent?.TextoBusca ?? string.Empty).Trim();
        if (busca.Length < 2)
            return await SalvarRespostaAsync(conversa, "texto", "Para editar uma despesa, informe o título ou estabelecimento e diga o que deseja alterar.", null, cancellationToken);

        var candidatas = await _context.Despesas.AsNoTracking()
            .Where(despesa => despesa.UsuariosEnvolvidos.Any(participante => participante.UsuarioId == conversa.UsuarioId))
            .Where(despesa =>
                (despesa.Titulo != null && despesa.Titulo.Contains(busca)) ||
                despesa.Descritivo.Contains(busca) ||
                despesa.TagsDespesas.Any(tagDespesa => tagDespesa.Tag.Nome.Contains(busca)))
            .OrderByDescending(despesa => despesa.Titulo == busca)
            .ThenByDescending(despesa => despesa.Vencimento)
            .Take(6)
            .Select(despesa => new
            {
                Id = despesa.Id,
                Titulo = despesa.Titulo,
                Descritivo = despesa.Descritivo,
                Valor = despesa.Valor,
                QtdParcelas = despesa.QtdParcelas,
                Vencimento = despesa.Vencimento,
                Recorrente = despesa.Recorrente
            })
            .ToListAsync(cancellationToken);

        if (candidatas.Count == 0)
            return await SalvarRespostaAsync(conversa, "texto", $"Não encontrei uma despesa associada à sua conta que corresponda a “{busca}”. Confira o título ou estabelecimento e tente novamente.", null, cancellationToken);

        if (candidatas.Count > 1)
        {
            string opcoesTexto = string.Join("\n", candidatas.Take(5).Select(despesa =>
                $"• {despesa.Titulo} — vencimento {despesa.Vencimento:dd/MM/yyyy}, total {despesa.Valor.GetValueOrDefault().ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}"));
            return await SalvarRespostaAsync(conversa, "texto", $"Encontrei mais de uma despesa que corresponde a “{busca}”. Qual delas você quer editar?\n{opcoesTexto}\n\nInforme um trecho mais específico do título ou a data de vencimento. Ainda não alterei nada.", null, cancellationToken);
        }

        if (intent == null || !TemCampoEdicaoSolicitado(intent))
            return await SalvarRespostaAsync(conversa, "texto", "Encontrei a despesa. Diga quais dados deseja alterar, como título, valor, vencimento, número de parcelas, tag ou participantes.", null, cancellationToken);

        Despesa? despesaSelecionada = await _context.ObterDespesaComAgregados(candidatas[0].Id);
        if (despesaSelecionada == null || !despesaSelecionada.UsuariosEnvolvidos.Any(item => item.UsuarioId == conversa.UsuarioId))
            return await SalvarRespostaAsync(conversa, "texto", "A despesa não está mais disponível para sua conta. Não alterei nenhuma informação.", null, cancellationToken);

        List<OcorrenciaDespesaRecorrente> ocorrenciasExistentes = await _context.OcorrenciasDespesasRecorrentes
            .AsNoTracking()
            .Where(item => item.DespesaId == despesaSelecionada.Id)
            .ToListAsync(cancellationToken);
        EdicaoDespesaViewModel edicao = EdicaoDespesaViewModel.Criar(despesaSelecionada);
        List<string> avisos = new();
        if (intent.Titulo != null) edicao.Titulo = intent.Titulo.Trim();
        if (intent.Descricao != null) edicao.Descritivo = intent.Descricao.Trim();
        if (intent.ValorParcela.HasValue) edicao.ValorParcela = intent.ValorParcela;
        if (intent.Vencimento.HasValue) edicao.Vencimento = intent.Vencimento.Value.ToDateTime(TimeOnly.MinValue);
        if (intent.QuantidadeParcelas.HasValue && !despesaSelecionada.Recorrente) edicao.QtdParcelas = intent.QuantidadeParcelas.Value;
        if (intent.Tags != null) edicao.Tags = intent.Tags.Select(tag => (tag ?? string.Empty).Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (intent.Participantes != null)
        {
            List<string> ids = new();
            foreach (string nome in intent.Participantes)
            {
                List<ChatOpcaoUsuario> encontrados = opcoes.Usuarios
                    .Where(usuario => string.Equals(usuario.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (encontrados.Count == 1) ids.Add(encontrados[0].Id);
                else avisos.Add($"Não consegui identificar de forma única o participante “{nome}”. Confira a seleção antes de salvar.");
            }
            if (avisos.Count == 0)
                edicao.UsuariosEnvolvidos = ids.Distinct(StringComparer.Ordinal).ToList();
        }

        ChatEdicaoRascunho rascunho = new()
        {
            Indice = 0,
            DespesaId = despesaSelecionada.Id,
            Titulo = edicao.Titulo ?? string.Empty,
            Descricao = edicao.Descritivo ?? string.Empty,
            ValorParcela = edicao.ValorParcela.GetValueOrDefault(),
            Vencimento = DateOnly.FromDateTime(edicao.Vencimento),
            QuantidadeParcelas = edicao.QtdParcelas.GetValueOrDefault(),
            Recorrente = despesaSelecionada.Recorrente,
            Tags = edicao.Tags.ToList(),
            Participantes = edicao.UsuariosEnvolvidos.ToList(),
            Avisos = avisos,
            AssinaturaOriginal = ChatEdicaoSnapshot.Assinar(despesaSelecionada, ocorrenciasExistentes)
        };

        string texto = $"Encontrei a despesa “{despesaSelecionada.Titulo}”. Revise os dados abaixo. A alteração só será aplicada quando você confirmar.";
        return await SalvarRespostaAsync(conversa, "edicao", texto, JsonSerializer.Serialize(new[] { rascunho }, JsonOptions), cancellationToken);
    }

    public async Task<ChatRespostaDto> ConfirmarEdicaoAsync(
        string usuarioId,
        int mensagemId,
        int indice,
        ChatConfirmarEdicao confirmacao,
        CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        ChatConversa? conversa = await _context.ChatConversas.SingleOrDefaultAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        ChatMensagem? mensagemEdicao = conversa == null
            ? null
            : await _context.ChatMensagens
                .FromSqlInterpolated($"SELECT * FROM ChatMensagens WITH (UPDLOCK, HOLDLOCK) WHERE Id = {mensagemId}")
                .SingleOrDefaultAsync(item => item.ConversaId == conversa.Id && item.Papel == "assistant" && item.Tipo == "edicao", cancellationToken);

        if (mensagemEdicao?.DadosJson == null)
            return new ChatRespostaDto(false, "Esta proposta de edição não está disponível para sua conta.", null, "edicao_nao_encontrada");

        List<ChatEdicaoRascunho> edicoes = JsonSerializer.Deserialize<List<ChatEdicaoRascunho>>(mensagemEdicao.DadosJson, JsonOptions) ?? new();
        ChatEdicaoRascunho? rascunho = edicoes.SingleOrDefault(item => item.Indice == indice);
        if (rascunho == null)
            return new ChatRespostaDto(false, "A proposta selecionada não existe mais.", null, "edicao_nao_encontrada");
        if (rascunho.Confirmado)
            return new ChatRespostaDto(false, "Esta proposta já foi aplicada.", null, "edicao_ja_confirmada");

        Despesa? despesa = await _context.Despesas
            .FromSqlInterpolated($"SELECT * FROM Despesas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {rascunho.DespesaId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (despesa == null)
            return new ChatRespostaDto(false, "A despesa não está mais disponível.", null, "despesa_nao_encontrada");

        despesa.Parcelas = await _context.Parcelas
            .FromSqlInterpolated($"SELECT * FROM Parcelas WITH (UPDLOCK, HOLDLOCK) WHERE DespesaId = {despesa.Id}")
            .ToListAsync(cancellationToken);
        await _context.Entry(despesa).Collection(item => item.UsuariosEnvolvidos).LoadAsync(cancellationToken);
        await _context.Entry(despesa).Collection(item => item.TagsDespesas).Query().Include(item => item.Tag).LoadAsync(cancellationToken);
        List<OcorrenciaDespesaRecorrente> ocorrencias = await _context.OcorrenciasDespesasRecorrentes
            .FromSqlInterpolated($"SELECT * FROM OcorrenciasDespesasRecorrentes WITH (UPDLOCK, HOLDLOCK) WHERE DespesaId = {despesa.Id}")
            .ToListAsync(cancellationToken);
        if (!despesa.UsuariosEnvolvidos.Any(item => item.UsuarioId == usuarioId))
            return new ChatRespostaDto(false, "Esta despesa não está associada à sua conta.", null, "despesa_nao_autorizada");
        if (!string.Equals(ChatEdicaoSnapshot.Assinar(despesa, ocorrencias), rascunho.AssinaturaOriginal, StringComparison.Ordinal))
            return new ChatRespostaDto(false, "Esta despesa foi alterada depois da proposta. Peça uma nova edição para revisar os dados atuais.", null, "edicao_desatualizada");

        EdicaoDespesaViewModel model = new()
        {
            Id = despesa.Id,
            Titulo = confirmacao.Titulo?.Trim(),
            Descritivo = confirmacao.Descricao?.Trim(),
            ValorParcela = confirmacao.ValorParcela,
            Vencimento = confirmacao.Vencimento?.ToDateTime(TimeOnly.MinValue) ?? default,
            QtdParcelas = despesa.Recorrente ? 0 : confirmacao.QuantidadeParcelas,
            Recorrente = despesa.Recorrente,
            UsuariosEnvolvidos = confirmacao.UsuariosIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList(),
            Tags = confirmacao.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };

        List<ValidationResult> validacao = new();
        Validator.TryValidateObject(model, new ValidationContext(model), validacao, validateAllProperties: true);
        if (validacao.Count > 0 || model.Vencimento == default || model.Titulo?.Length > 100 || model.Descritivo?.Length > 500 ||
            !model.ValorParcela.HasValue || model.ValorParcela.Value > 99999999999999.99m ||
            (!despesa.Recorrente && model.QtdParcelas is < 1 or > 120))
            return new ChatRespostaDto(false, "Confira o título, a descrição, o valor, o vencimento e a quantidade de parcelas antes de salvar.", null, "edicao_invalida");

        int quantidadeUsuariosValidos = await _context.Users.CountAsync(item => model.UsuariosEnvolvidos.Contains(item.Id), cancellationToken);
        if (model.UsuariosEnvolvidos.Count == 0 || quantidadeUsuariosValidos != model.UsuariosEnvolvidos.Count)
            return new ChatRespostaDto(false, "Selecione ao menos um participante válido.", null, "participantes_invalidos");

        List<string> nomesTags = model.Tags.Where(nome => nome.Length > 0).ToList();
        if (nomesTags.Any(nome => nome.Length > 100))
            return new ChatRespostaDto(false, "Cada tag deve ter até 100 caracteres.", null, "tags_invalidas");
        List<Tag> tagsEncontradas = await _context.Tags.Where(tag => nomesTags.Contains(tag.Nome)).ToListAsync(cancellationToken);
        List<Tag> tagsNovas = nomesTags.Except(tagsEncontradas.Select(tag => tag.Nome), StringComparer.OrdinalIgnoreCase)
            .Select(nome => new Tag(nome)).ToList();
        if (tagsNovas.Count > 0)
        {
            _context.Tags.AddRange(tagsNovas);
            await _context.SaveChangesAsync(cancellationToken);
        }

        model.AdicionarTags(tagsEncontradas.Concat(tagsNovas).ToList());
        despesa.Atualizar(model);
        _context.Update(despesa);
        await _context.SaveChangesAsync(cancellationToken);

        rascunho.Titulo = model.Titulo!;
        rascunho.Descricao = model.Descritivo ?? string.Empty;
        rascunho.ValorParcela = model.ValorParcela!.Value;
        rascunho.Vencimento = DateOnly.FromDateTime(model.Vencimento);
        rascunho.QuantidadeParcelas = model.QtdParcelas.GetValueOrDefault();
        rascunho.Tags = model.Tags.ToList();
        rascunho.Participantes = model.UsuariosEnvolvidos.ToList();
        rascunho.Confirmado = true;
        mensagemEdicao.DadosJson = JsonSerializer.Serialize(edicoes, JsonOptions);
        mensagemEdicao.Conteudo += $"\n\nDespesa “{model.Titulo}” atualizada com sucesso.";
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return new ChatRespostaDto(true, "Despesa atualizada com sucesso.", MapearMensagem(mensagemEdicao));
    }

    private static bool TemCampoEdicaoSolicitado(ChatEdicaoIntent intent) =>
        intent.Titulo != null || intent.Descricao != null || intent.ValorParcela.HasValue || intent.Vencimento.HasValue ||
        intent.QuantidadeParcelas.HasValue || intent.Tags != null || intent.Participantes != null;

    private async Task BloquearVerificacaoDuplicidadeAsync(
        IEnumerable<ChatChaveDuplicidade> chaves,
        CancellationToken cancellationToken)
    {
        List<string> recursos = chaves
            .SelectMany(chave => chave.Participantes.SelectMany(participanteId =>
                new[] { chave.ValorParcela, chave.ValorTotal }
                    .Distinct()
                    .Select(valor => CriarRecursoBloqueioDuplicidade(chave.Data, valor, participanteId))))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(recurso => recurso, StringComparer.Ordinal)
            .ToList();

        foreach (string recurso in recursos)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DECLARE @resultado int; EXEC @resultado = sp_getapplock @Resource = {recurso}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @resultado < 0 THROW 51001, 'Could not acquire chat expense duplicate lock.', 1;",
                cancellationToken);
        }
    }

    private async Task<bool> ExistePossivelDuplicidadeAsync(
        ChatChaveDuplicidade chave,
        CancellationToken cancellationToken)
    {
        DateTime inicio = chave.Data.Date;
        DateTime fim = inicio.AddDays(1);
        bool despesaCompletaExistente = await _context.Despesas.AsNoTracking().AnyAsync(despesa =>
            despesa.Vencimento >= inicio && despesa.Vencimento < fim &&
            despesa.Valor == chave.ValorTotal &&
            despesa.UsuariosEnvolvidos.Any(participante => chave.Participantes.Contains(participante.UsuarioId)),
            cancellationToken);
        if (despesaCompletaExistente)
            return true;

        bool parcelaExistente = await _context.Parcelas.AsNoTracking().AnyAsync(parcela =>
            parcela.Vencimento >= inicio && parcela.Vencimento < fim &&
            parcela.Valor == chave.ValorParcela &&
            parcela.Despesa.UsuariosEnvolvidos.Any(participante => chave.Participantes.Contains(participante.UsuarioId)),
            cancellationToken);
        if (parcelaExistente)
            return true;

        bool ocorrenciaExistente = await _context.OcorrenciasDespesasRecorrentes.AsNoTracking().AnyAsync(ocorrencia =>
            ocorrencia.Vencimento >= inicio && ocorrencia.Vencimento < fim &&
            ocorrencia.Valor == chave.ValorParcela &&
            ocorrencia.Despesa.UsuariosEnvolvidos.Any(participante => chave.Participantes.Contains(participante.UsuarioId)),
            cancellationToken);
        if (ocorrenciaExistente)
            return true;

        List<DateTime> datasBaseRecorrencia = await _context.Despesas.AsNoTracking()
            .Where(despesa => despesa.Recorrente && despesa.Valor == chave.ValorParcela &&
                despesa.Vencimento <= fim &&
                despesa.UsuariosEnvolvidos.Any(participante => chave.Participantes.Contains(participante.UsuarioId)))
            .Select(despesa => despesa.Vencimento)
            .ToListAsync(cancellationToken);

        DateTime mesConsulta = new(inicio.Year, inicio.Month, 1);
        return datasBaseRecorrencia.Any(dataBase =>
        {
            DateTime mesInicio = new(dataBase.Year, dataBase.Month, 1);
            int diaVencimento = Math.Min(dataBase.Day, DateTime.DaysInMonth(inicio.Year, inicio.Month));
            DateTime vencimentoPrevisto = new(inicio.Year, inicio.Month, diaVencimento);
            return mesInicio <= mesConsulta && vencimentoPrevisto == inicio;
        });
    }

    private static string CriarRecursoBloqueioDuplicidade(DateTime data, decimal valor, string participanteId)
    {
        string chave = string.Join('|',
            data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            valor.ToString("F2", CultureInfo.InvariantCulture),
            participanteId);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave)));
        return $"ChatDespesaDuplicidade:{hash}";
    }

    private sealed record ChatChaveDuplicidade(
        DateTime Data,
        decimal ValorParcela,
        decimal ValorTotal,
        IReadOnlyCollection<string> Participantes);

    private async Task<ChatRespostaDto> ProcessarConsultaAsync(
        ChatConversa conversa,
        ChatConsultaIntent consulta,
        CancellationToken cancellationToken)
    {
        DateOnly hoje = DateOnly.FromDateTime(DateTime.Today);
        DateOnly inicio = consulta.Inicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
        DateOnly fim = consulta.Fim ?? new DateOnly(inicio.Year, inicio.Month, DateTime.DaysInMonth(inicio.Year, inicio.Month));
        if (inicio > fim)
            return await SalvarRespostaAsync(conversa, "texto", "O início do período ficou depois do fim. Pode informar as datas novamente?", null, cancellationToken);
        bool temDataComparacao = consulta.InicioComparacao.HasValue || consulta.FimComparacao.HasValue;
        if (temDataComparacao && (!consulta.InicioComparacao.HasValue || !consulta.FimComparacao.HasValue || consulta.InicioComparacao > consulta.FimComparacao))
            return await SalvarRespostaAsync(conversa, "texto", "Não consegui determinar as duas datas do período de comparação. Pode informar os meses que deseja comparar?", null, cancellationToken);

        if (PeriodoExcedeLimite(inicio, fim) ||
            (consulta.InicioComparacao.HasValue && consulta.FimComparacao.HasValue &&
                PeriodoExcedeLimite(consulta.InicioComparacao.Value, consulta.FimComparacao.Value)))
            return await SalvarRespostaAsync(conversa, "texto", "O período deve ter no máximo dez anos. Informe um intervalo menor para gerar o relatório.", null, cancellationToken);

        List<string> tagsSolicitadas = (consulta.Tags ?? new List<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        (List<int> tags, bool todasEncontradas) = await ObterIdsTagsAsync(tagsSolicitadas, cancellationToken);
        if (!todasEncontradas)
            return await SalvarRespostaAsync(conversa, "texto", "Não encontrei todas as tags informadas. Escolha tags existentes ou faça a consulta sem esse filtro.", null, cancellationToken);

        DateTime inicioDate = inicio.ToDateTime(TimeOnly.MinValue);
        DateTime fimDate = fim.ToDateTime(TimeOnly.MaxValue);
        await _context.GarantirOcorrenciasRecorrentesNoPeriodo(inicioDate, fimDate, cancellationToken);
        if (consulta.InicioComparacao.HasValue && consulta.FimComparacao.HasValue)
        {
            DateTime comparacaoInicio = consulta.InicioComparacao.Value.ToDateTime(TimeOnly.MinValue);
            DateTime comparacaoFim = consulta.FimComparacao.Value.ToDateTime(TimeOnly.MaxValue);
            await _context.GarantirOcorrenciasRecorrentesNoPeriodo(comparacaoInicio, comparacaoFim, cancellationToken);
        }

        List<ChatLinhaDespesa> itens = await ObterItensAsync(conversa.UsuarioId, inicioDate, fimDate, consulta.Texto, tags, cancellationToken);
        itens = AplicarSituacao(itens, consulta.Situacao, hoje);

        ChatPayloadConsulta payload = CriarPayload(conversa.UsuarioId, inicio, fim, itens, consulta.AgruparPor);
        ChatPayloadComparacao? comparacao = null;
        if (consulta.InicioComparacao.HasValue && consulta.FimComparacao.HasValue && consulta.InicioComparacao <= consulta.FimComparacao)
        {
            DateTime comparacaoInicio = consulta.InicioComparacao.Value.ToDateTime(TimeOnly.MinValue);
            DateTime comparacaoFim = consulta.FimComparacao.Value.ToDateTime(TimeOnly.MaxValue);
            List<ChatLinhaDespesa> itensComparacao = await ObterItensAsync(conversa.UsuarioId, comparacaoInicio, comparacaoFim, consulta.Texto, tags, cancellationToken);
            itensComparacao = AplicarSituacao(itensComparacao, consulta.Situacao, hoje);
            comparacao = new ChatPayloadComparacao(
                FormatarPeriodo(inicio, fim),
                itens.Sum(item => item.Valor),
                payload.ParteUsuario,
                FormatarPeriodo(consulta.InicioComparacao.Value, consulta.FimComparacao.Value),
                itensComparacao.Sum(item => item.Valor),
                itensComparacao.Sum(item => ChatRateio.ObterParteDoUsuario(item.Valor,
                    item.Despesa.UsuariosEnvolvidos.Select(envolvido => (envolvido.UsuarioId, NomeUsuario(envolvido.Usuario))).ToList(), conversa.UsuarioId)),
                itens.Sum(item => item.Valor) - itensComparacao.Sum(item => item.Valor));
            payload = payload with { Comparacao = comparacao };
        }

        string conteudo = CriarResumo(payload);
        return await SalvarRespostaAsync(conversa, "consulta", conteudo, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
    }

    private async Task<List<ChatLinhaDespesa>> ObterItensAsync(
        string usuarioId,
        DateTime inicio,
        DateTime fim,
        string? texto,
        IReadOnlyList<int> tags,
        CancellationToken cancellationToken)
    {
        string termo = (texto ?? string.Empty).Trim();
        List<string> usuarios = new() { usuarioId };
        List<Parcela> parcelas = await _context.ObterParcelasFiltradas(usuarios, termo, inicio, fim, tags)
            .AsTracking()
            .ToListAsync(cancellationToken);
        List<OcorrenciaDespesaRecorrente> ocorrencias = await _context.ObterOcorrenciasRecorrentesFiltradas(usuarios, termo, inicio, fim, tags)
            .ToListAsync(cancellationToken);

        List<ChatLinhaDespesa> itens = parcelas.Select(parcela => new ChatLinhaDespesa(
                parcela.Despesa,
                parcela.Vencimento,
                parcela.Valor,
                parcela.Pago))
            .Concat(ocorrencias.Select(ocorrencia => new ChatLinhaDespesa(
                ocorrencia.Despesa,
                ocorrencia.Vencimento,
                ocorrencia.Valor,
                ocorrencia.Pago)))
            .OrderBy(item => item.Data)
            .ThenBy(item => item.Despesa.Titulo)
            .ToList();

        return itens;
    }

    private static List<ChatLinhaDespesa> AplicarSituacao(List<ChatLinhaDespesa> itens, string? situacao, DateOnly hoje)
    {
        string status = (situacao ?? "todas").Trim().ToLowerInvariant();
        return status switch
        {
            "pagas" or "paga" => itens.Where(item => item.Pago).ToList(),
            "abertas" or "aberta" => itens.Where(item => !item.Pago).ToList(),
            "pendentes" or "pendente" => itens.Where(item => !item.Pago && DateOnly.FromDateTime(item.Data) >= hoje).ToList(),
            "vencidas" or "vencida" => itens.Where(item => !item.Pago && DateOnly.FromDateTime(item.Data) < hoje).ToList(),
            _ => itens
        };
    }

    private static ChatPayloadConsulta CriarPayload(string usuarioAtualId, DateOnly inicio, DateOnly fim, List<ChatLinhaDespesa> itens, string? agruparPor)
    {
        List<ChatItemConsulta> itensDto = itens.Select(item =>
        {
            List<(string Id, string Nome)> participantes = item.Despesa.UsuariosEnvolvidos
                .Select(envolvido => (envolvido.UsuarioId, NomeUsuario(envolvido.Usuario)))
                .OrderBy(envolvido => envolvido.UsuarioId, StringComparer.Ordinal)
                .ToList();

            List<ChatParteUsuario> partes = ChatRateio.Dividir(item.Valor, participantes);
            decimal parteAtual = ChatRateio.ObterParteDoUsuario(item.Valor, participantes, usuarioAtualId);
            string situacao = item.Pago ? "Paga" : item.Data.Date < DateTime.Today ? "Vencida" : "Pendente";
            return new ChatItemConsulta(
                item.Despesa.Titulo ?? item.Despesa.Descritivo,
                item.Data.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")),
                situacao,
                item.Valor,
                parteAtual,
                partes,
                item.Despesa.TagsDespesas.Select(tag => tag.Tag.Nome).OrderBy(nome => nome).ToList());
        }).ToList();

        decimal total = itens.Sum(item => item.Valor);
        decimal parteUsuario = itensDto.Sum(item => item.ParteUsuario);
        List<ChatGrupoConsulta> grupos = new();
        if (string.Equals(agruparPor, "tag", StringComparison.OrdinalIgnoreCase))
        {
            grupos = itens
                .SelectMany(item =>
                {
                    string[] tagsItem = item.Despesa.TagsDespesas.Select(tag => tag.Tag.Nome).Distinct().ToArray();
                    if (tagsItem.Length == 0)
                        tagsItem = new[] { "Sem tag" };
                    List<(string Id, string Nome)> participantes = item.Despesa.UsuariosEnvolvidos
                        .Select(envolvido => (envolvido.UsuarioId, NomeUsuario(envolvido.Usuario)))
                        .OrderBy(envolvido => envolvido.UsuarioId, StringComparer.Ordinal)
                        .ToList();
                    List<ChatParteUsuario> partes = ChatRateio.Dividir(item.Valor, participantes);
                    decimal parteAtual = ChatRateio.ObterParteDoUsuario(item.Valor, participantes, usuarioAtualId);
                    return tagsItem.Select(tag => new { Tag = tag, item.Valor, Parte = parteAtual });
                })
                .GroupBy(item => item.Tag)
                .OrderByDescending(grupo => grupo.Sum(item => item.Valor))
                .Select(grupo => new ChatGrupoConsulta(grupo.Key, grupo.Sum(item => item.Valor), grupo.Sum(item => item.Parte), grupo.Count()))
                .ToList();
        }

        List<ChatGrupoConsulta> situacoes = itensDto
            .GroupBy(item => item.Situacao)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new ChatGrupoConsulta(
                grupo.Key,
                grupo.Sum(item => item.ValorTotal),
                grupo.Sum(item => item.ParteUsuario),
                grupo.Count()))
            .ToList();

        return new ChatPayloadConsulta(FormatarPeriodo(inicio, fim), total, parteUsuario, itens.Count, itensDto, grupos, situacoes);
    }

    private static string CriarResumo(ChatPayloadConsulta payload)
    {
        CultureInfo cultura = CultureInfo.GetCultureInfo("pt-BR");
        string resumo = $"No período {payload.Periodo}, encontrei {payload.Quantidade} lançamento(s). O total integral é {payload.ValorTotal.ToString("C", cultura)} e a sua parte é {payload.ParteUsuario.ToString("C", cultura)}.";
        if (payload.Comparacao != null)
        {
            decimal variacao = payload.Comparacao.Diferenca;
            string direcao = variacao > 0 ? "a mais" : variacao < 0 ? "a menos" : "sem diferença";
            resumo += $"\nComparação: {payload.Comparacao.PeriodoAtual} somou {payload.Comparacao.TotalAtual.ToString("C", cultura)} (sua parte: {payload.Comparacao.ParteUsuarioAtual.ToString("C", cultura)}); {payload.Comparacao.PeriodoComparado} somou {payload.Comparacao.TotalComparado.ToString("C", cultura)} (sua parte: {payload.Comparacao.ParteUsuarioComparada.ToString("C", cultura)}). A diferença integral foi {Math.Abs(variacao).ToString("C", cultura)} {direcao}.";
        }
        if (payload.Grupos.Count > 0)
            resumo += "\nA tabela abaixo agrupa por tag. Uma despesa com mais de uma tag aparece em cada grupo correspondente.";
        if (payload.Quantidade == 0)
            resumo += "\nNão há lançamentos que correspondam aos filtros informados.";
        return resumo;
    }

    private async Task<(List<int> Ids, bool TodasEncontradas)> ObterIdsTagsAsync(IReadOnlyList<string> nomes, CancellationToken cancellationToken)
    {
        List<string> normalizados = nomes.Where(nome => !string.IsNullOrWhiteSpace(nome)).Select(nome => nome.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (normalizados.Count == 0)
            return (new(), true);

        List<Tag> encontradas = await _context.Tags.AsNoTracking()
            .Where(tag => normalizados.Contains(tag.Nome))
            .ToListAsync(cancellationToken);
        bool todasEncontradas = normalizados.All(nome => encontradas.Any(tag => string.Equals(tag.Nome, nome, StringComparison.OrdinalIgnoreCase)));
        return (encontradas.Select(tag => tag.Id).ToList(), todasEncontradas);
    }

    private async Task<ChatOpcoes> ObterOpcoesAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var usuariosDados = await _context.Users.AsNoTracking()
            .OrderBy(usuario => usuario.Nome)
            .ThenBy(usuario => usuario.SobreNome)
            .ToListAsync(cancellationToken);
        List<ChatOpcaoUsuario> usuarios = usuariosDados
            .Select(usuario => new ChatOpcaoUsuario(
                usuario.Id,
                $"{usuario.Nome} {usuario.SobreNome}".Trim() is { Length: > 0 } nome ? nome : usuario.UserName ?? "Usuário"))
            .ToList();
        List<string> tags = await _context.Tags.AsNoTracking().OrderBy(tag => tag.Nome).Select(tag => tag.Nome).ToListAsync(cancellationToken);
        return new ChatOpcoes(usuarios, tags, usuarioId);
    }

    private async Task<ChatConversa> ObterConversaAsync(string usuarioId, CancellationToken cancellationToken)
    {
        ChatConversa? conversa = await _context.ChatConversas.SingleOrDefaultAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        if (conversa != null)
            return conversa;

        conversa = new ChatConversa(usuarioId, DateTime.Now);
        _context.ChatConversas.Add(conversa);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return conversa;
        }
        catch (DbUpdateException)
        {
            _context.Entry(conversa).State = EntityState.Detached;
            return await _context.ChatConversas.SingleAsync(item => item.UsuarioId == usuarioId, cancellationToken);
        }
    }

    private async Task<ChatRespostaDto> SalvarRespostaAsync(
        ChatConversa conversa,
        string tipo,
        string conteudo,
        string? dadosJson,
        CancellationToken cancellationToken)
    {
        ChatMensagem mensagem = new(conversa.Id, "assistant", tipo, conteudo, DateTime.Now, dadosJson);
        _context.ChatMensagens.Add(mensagem);
        await _context.SaveChangesAsync(cancellationToken);
        return new ChatRespostaDto(true, conteudo, MapearMensagem(mensagem));
    }

    private static ChatMensagemDto MapearMensagem(ChatMensagem mensagem)
    {
        List<ChatRascunho>? rascunhos = mensagem.Tipo == "rascunho" && mensagem.DadosJson != null
            ? JsonSerializer.Deserialize<List<ChatRascunho>>(mensagem.DadosJson, JsonOptions)
            : null;
        ChatPayloadConsulta? consulta = mensagem.Tipo == "consulta" && mensagem.DadosJson != null
            ? JsonSerializer.Deserialize<ChatPayloadConsulta>(mensagem.DadosJson, JsonOptions)
            : null;
        List<ChatEdicaoRascunho>? edicoes = mensagem.Tipo == "edicao" && mensagem.DadosJson != null
            ? JsonSerializer.Deserialize<List<ChatEdicaoRascunho>>(mensagem.DadosJson, JsonOptions)
            : null;
        return new ChatMensagemDto(mensagem.Id, mensagem.Papel, mensagem.Tipo, mensagem.Conteudo, mensagem.CriadaEm, rascunhos, consulta, edicoes);
    }

    private static string ObterRespostaConversacional(ChatInterpretacao interpretacao)
    {
        string resposta = interpretacao.Mensagem.Trim();
        if (string.IsNullOrWhiteSpace(resposta) || resposta.StartsWith("thinking", StringComparison.OrdinalIgnoreCase))
            return "Posso ajudar a consultar despesas ou preparar um novo lançamento. O que você gostaria de fazer?";
        return resposta.Length <= 4000 ? resposta : resposta[..4000];
    }

    private static string NomeUsuario(Usuario usuario)
    {
        string nome = $"{usuario.Nome} {usuario.SobreNome}".Trim();
        return nome.Length > 0 ? nome : usuario.UserName ?? "Usuário";
    }

    private static string FormatarPeriodo(DateOnly inicio, DateOnly fim)
    {
        CultureInfo cultura = CultureInfo.GetCultureInfo("pt-BR");
        if (inicio.Year == fim.Year && inicio.Month == fim.Month)
            return inicio.ToDateTime(TimeOnly.MinValue).ToString("MMMM 'de' yyyy", cultura);
        return $"{inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
    }

    private static bool PeriodoExcedeLimite(DateOnly inicio, DateOnly fim)
    {
        int diferencaAnos = fim.Year - inicio.Year;
        return diferencaAnos > 10 ||
            (diferencaAnos == 10 &&
                (fim.Month > inicio.Month || (fim.Month == inicio.Month && fim.Day > inicio.Day)));
    }

    private sealed record ChatLinhaDespesa(Despesa Despesa, DateTime Data, decimal Valor, bool Pago);
}

public sealed record ChatCarregamentoDto(IReadOnlyList<ChatMensagemDto> Mensagens, ChatOpcoes Opcoes);
