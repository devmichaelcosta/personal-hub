(() => {
    const widget = document.getElementById("chat-widget");
    if (!widget) return;

    const panel = document.getElementById("chat-panel");
    const launcher = document.getElementById("chat-launcher");
    const messages = document.getElementById("chat-messages");
    const status = document.getElementById("chat-status");
    const form = document.getElementById("chat-form");
    const input = document.getElementById("chat-input");
    const imageInput = document.getElementById("chat-image");
    const fileName = document.getElementById("chat-file-name");
    const sendButton = document.getElementById("chat-send");
    const csrfToken = widget.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    const openStorageKey = "despesas.chat.aberto";
    const state = { opcoes: null, busy: false };

    const setOpen = (open, focusComposer = false) => {
        panel.hidden = !open;
        launcher.hidden = open;
        localStorage.setItem(openStorageKey, open ? "true" : "false");
        if (open && focusComposer) input.focus();
    };

    function criarElemento(tag, className, text) {
        const element = document.createElement(tag);
        if (className) element.className = className;
        if (text !== undefined) element.textContent = text;
        return element;
    }

    function irParaOFim() {
        messages.scrollTop = messages.scrollHeight;
    }

    function adicionarMensagemUsuario(texto, nomeArquivo) {
        const artigo = criarElemento("article", "chat-message chat-message-user");
        artigo.append(criarElemento("div", "chat-bubble", texto || "Imagem anexada"));
        if (nomeArquivo) artigo.append(criarElemento("span", "chat-attachment-note", `Imagem: ${nomeArquivo}`));
        messages.append(artigo);
        irParaOFim();
    }

    function renderizarTabelaConsulta(container, consulta) {
        if (!consulta) return;

        if (consulta.situacoes?.length) {
            const heading = criarElemento("h4", "chat-table-heading", "Resumo por situação");
            const wrapper = criarElemento("div", "chat-table-wrap");
            const tabela = criarElemento("table", "chat-data-table");
            const thead = document.createElement("thead");
            const cabecalho = document.createElement("tr");
            ["Situação", "Lançamentos", "Total integral", "Sua parte"].forEach(texto => cabecalho.append(criarElemento("th", "", texto)));
            thead.append(cabecalho);
            const tbody = document.createElement("tbody");
            consulta.situacoes.forEach(situacao => {
                const linha = document.createElement("tr");
                [situacao.nome, situacao.quantidade, formatarMoeda(situacao.valorTotal), formatarMoeda(situacao.parteUsuario)]
                    .forEach(valor => linha.append(criarElemento("td", "", String(valor))));
                tbody.append(linha);
            });
            tabela.append(thead, tbody);
            wrapper.append(tabela);
            container.append(heading, wrapper);
        }

        if (consulta.grupos?.length) {
            const heading = criarElemento("h4", "chat-table-heading", "Resumo por tag");
            const wrapper = criarElemento("div", "chat-table-wrap");
            const tabela = criarElemento("table", "chat-data-table");
            const thead = document.createElement("thead");
            const cabecalho = document.createElement("tr");
            ["Tag", "Lançamentos", "Total integral", "Sua parte"].forEach(texto => cabecalho.append(criarElemento("th", "", texto)));
            thead.append(cabecalho);
            const tbody = document.createElement("tbody");
            consulta.grupos.forEach(grupo => {
                const linha = document.createElement("tr");
                [grupo.nome, grupo.quantidade, formatarMoeda(grupo.valorTotal), formatarMoeda(grupo.parteUsuario)]
                    .forEach(valor => linha.append(criarElemento("td", "", String(valor))));
                tbody.append(linha);
            });
            tabela.append(thead, tbody);
            wrapper.append(tabela);
            container.append(heading, wrapper);
        }

        if (!consulta.itens?.length) return;
        const wrapper = criarElemento("div", "chat-table-wrap");
        const tabela = criarElemento("table", "chat-data-table chat-items-table");
        const thead = document.createElement("thead");
        const cabecalho = document.createElement("tr");
        ["Data", "Despesa", "Situação", "Total", "Sua parte", "Partes"].forEach(texto => cabecalho.append(criarElemento("th", "", texto)));
        thead.append(cabecalho);
        const tbody = document.createElement("tbody");
        consulta.itens.forEach(item => {
            const linha = document.createElement("tr");
            const partes = (item.partes ?? []).map(parte => `${parte.nome}: ${formatarMoeda(parte.valor)}`).join(" · ");
            const valores = [item.data, item.titulo, item.situacao, formatarMoeda(item.valorTotal), formatarMoeda(item.parteUsuario), partes];
            valores.forEach(valor => linha.append(criarElemento("td", "", String(valor ?? ""))));
            tbody.append(linha);
        });
        tabela.append(thead, tbody);
        wrapper.append(tabela);
        container.append(wrapper);
    }

    function formatarMoeda(valor) {
        return Number(valor ?? 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
    }

    function criarCampo(label, name, value, type, options = {}) {
        const wrapper = criarElemento("label", "chat-draft-field");
        wrapper.append(criarElemento("span", "", label));
        const control = document.createElement(type === "textarea" ? "textarea" : "input");
        control.name = name;
        if (type !== "textarea") control.type = type;
        control.value = value ?? "";
        if (options.step) control.step = options.step;
        if (options.min !== undefined) control.min = options.min;
        if (options.max !== undefined) control.max = options.max;
        if (options.maxLength) control.maxLength = options.maxLength;
        wrapper.append(control);
        return wrapper;
    }

    function criarRascunho(mensagemId, rascunho) {
        const card = criarElemento("section", `chat-draft-card${rascunho.confirmado ? " is-confirmed" : ""}`);
        card.dataset.draftIndex = String(rascunho.indice);
        card.append(criarElemento("h3", "", rascunho.confirmado ? "Despesa cadastrada" : `Revisar lançamento ${rascunho.indice + 1}`));
        if (rascunho.confirmado) {
            card.append(criarElemento("p", "chat-confirmed-note", `Registrada com sucesso (despesa ${rascunho.despesaId ?? ""}).`));
            return card;
        }

        (rascunho.avisos ?? []).forEach(aviso => card.append(criarElemento("p", "chat-draft-warning", aviso)));

        const fields = criarElemento("div", "chat-draft-fields");
        const title = criarCampo("Título", "titulo", rascunho.titulo, "text", { maxLength: 100 });
        const description = criarCampo("Descrição", "descricao", rascunho.descricao, "textarea", { maxLength: 500 });
        const amount = criarCampo("Valor por parcela (R$)", "valorParcela", rascunho.valorParcela, "number", { step: "0.01", min: "0.01" });
        const date = criarCampo("Vencimento / data da compra", "vencimento", rascunho.vencimento ?? "", "date");
        const quantity = criarCampo("Quantidade de parcelas", "quantidadeParcelas", rascunho.quantidadeParcelas || 1, "number", { min: "1", max: "120", step: "1" });
        fields.append(title, description, amount, date, quantity);

        const tagLabel = criarElemento("label", "chat-draft-field");
        tagLabel.append(criarElemento("span", "", "Tag"));
        const tagSelect = document.createElement("select");
        tagSelect.name = "tag";
        tagSelect.append(new Option("Sem tag", ""));
        (state.opcoes?.tags ?? []).forEach(tag => tagSelect.append(new Option(tag, tag)));
        tagSelect.value = rascunho.tag ?? "";
        tagLabel.append(tagSelect);
        fields.append(tagLabel);

        const flags = criarElemento("div", "chat-draft-flags");
        flags.append(criarCheck("Despesa recorrente mensal", "recorrente", rascunho.recorrente));
        flags.append(criarCheck("Já foi paga", "pago", rascunho.pago));
        fields.append(flags);

        const participantGroup = criarElemento("fieldset", "chat-draft-participants");
        participantGroup.append(criarElemento("legend", "", "Envolvidos"));
        const selected = new Set(rascunho.participantes ?? []);
        if (selected.size === 0 && state.opcoes?.usuarioAtualId) selected.add(state.opcoes.usuarioAtualId);
        (state.opcoes?.usuarios ?? []).forEach(usuario => {
            const label = criarElemento("label", "chat-participant-option");
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.name = "usuariosIds";
            checkbox.value = usuario.id;
            checkbox.checked = selected.has(usuario.id);
            label.append(checkbox, document.createTextNode(usuario.nome));
            participantGroup.append(label);
        });
        fields.append(participantGroup);

        const button = criarElemento("button", "chat-draft-confirm", "Confirmar despesa");
        button.type = "button";
        button.addEventListener("click", () => confirmarRascunho(button, mensagemId, rascunho.indice, fields));
        card.append(fields, button);
        return card;
    }

    function criarCheck(text, name, checked) {
        const label = criarElemento("label", "chat-check-option");
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.name = name;
        checkbox.checked = Boolean(checked);
        label.append(checkbox, document.createTextNode(text));
        return label;
    }

    function criarEdicao(mensagemId, edicao) {
        const card = criarElemento("section", `chat-draft-card chat-edit-card${edicao.confirmado ? " is-confirmed" : ""}`);
        card.dataset.editIndex = String(edicao.indice);
        card.append(criarElemento("h3", "", edicao.confirmado ? "Despesa atualizada" : `Editar despesa ${edicao.titulo}`));
        if (edicao.confirmado) {
            card.append(criarElemento("p", "chat-confirmed-note", "Alteração salva com sucesso."));
            return card;
        }

        (edicao.avisos ?? []).forEach(aviso => card.append(criarElemento("p", "chat-draft-warning", aviso)));
        const fields = criarElemento("div", "chat-draft-fields");
        fields.dataset.editFields = "true";
        fields.append(
            criarCampo("Título", "titulo", edicao.titulo, "text", { maxLength: 100 }),
            criarCampo("Descrição", "descricao", edicao.descricao, "textarea", { maxLength: 500 }),
            criarCampo(edicao.recorrente ? "Valor mensal (R$)" : "Valor por parcela (R$)", "valorParcela", edicao.valorParcela, "number", { step: "0.01", min: "0.01" }),
            criarCampo(edicao.recorrente ? "Vencimento inicial" : "Vencimento", "vencimento", edicao.vencimento, "date")
        );
        if (!edicao.recorrente) {
            fields.append(criarCampo("Quantidade de parcelas", "quantidadeParcelas", edicao.quantidadeParcelas, "number", { min: "1", max: "120", step: "1" }));
        }
        fields.append(criarCampo("Tags (separadas por vírgula)", "tags", (edicao.tags ?? []).join(", "), "text", { maxLength: 1000 }));

        const participantGroup = criarElemento("fieldset", "chat-draft-participants");
        participantGroup.append(criarElemento("legend", "", "Envolvidos"));
        const selected = new Set(edicao.participantes ?? []);
        (state.opcoes?.usuarios ?? []).forEach(usuario => {
            const label = criarElemento("label", "chat-participant-option");
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.name = "usuariosIds";
            checkbox.value = usuario.id;
            checkbox.checked = selected.has(usuario.id);
            label.append(checkbox, document.createTextNode(usuario.nome));
            participantGroup.append(label);
        });
        fields.append(participantGroup);

        const button = criarElemento("button", "chat-draft-confirm", "Salvar alteração");
        button.type = "button";
        button.addEventListener("click", () => confirmarEdicao(button, mensagemId, edicao.indice, fields));
        card.append(fields, button);
        return card;
    }

    async function confirmarEdicao(button, mensagemId, indice, fields) {
        const valor = name => fields.querySelector(`[name="${name}"]`)?.value ?? "";
        const valorParcela = valor("valorParcela");
        const tags = valor("tags").split(",").map(tag => tag.trim()).filter(Boolean);
        const confirmacao = {
            titulo: valor("titulo"),
            descricao: valor("descricao"),
            valorParcela: valorParcela !== "" && Number.isFinite(Number(valorParcela)) ? Number(valorParcela) : null,
            vencimento: valor("vencimento") || null,
            quantidadeParcelas: Number.parseInt(valor("quantidadeParcelas"), 10) || 0,
            tags,
            usuariosIds: Array.from(fields.querySelectorAll('input[name="usuariosIds"]:checked'), control => control.value)
        };
        const formData = new FormData();
        formData.append("__RequestVerificationToken", csrfToken);
        formData.append("mensagemId", String(mensagemId));
        formData.append("indice", String(indice));
        formData.append("edicaoJson", JSON.stringify(confirmacao));
        button.disabled = true;

        try {
            const response = await fetch(widget.dataset.confirmEditUrl, { method: "POST", body: formData, credentials: "same-origin" });
            const result = await response.json();
            if (!response.ok || !result.sucesso) throw new Error(result.mensagem || "Não foi possível atualizar a despesa.");
            await carregarHistorico();
            definirStatus("Despesa atualizada.");
        } catch (error) {
            adicionarMensagemAssistente(String(error.message ?? error));
        } finally {
            button.disabled = false;
        }
    }

    async function confirmarRascunho(button, mensagemId, indice, fields, ignorarPossivelDuplicidade = false) {
        const formData = new FormData();
        formData.append("__RequestVerificationToken", csrfToken);
        formData.append("mensagemId", String(mensagemId));
        const confirmacao = coletarConfirmacao(fields, indice);
        Object.entries(confirmacao).forEach(([name, value]) => {
            if (name === "usuariosIds") value.forEach(id => formData.append(name, id));
            else formData.append(name, value ?? "");
        });
        formData.append("ignorarPossivelDuplicidade", ignorarPossivelDuplicidade ? "true" : "false");
        button.disabled = true;

        try {
            const response = await fetch(widget.dataset.confirmUrl, { method: "POST", body: formData, credentials: "same-origin" });
            const result = await response.json();
            if (!response.ok && result.erro === "possivel_duplicidade") {
                const prosseguir = window.confirm(`${result.mensagem}\n\nDeseja registrar mesmo assim?`);
                if (prosseguir) return confirmarRascunho(button, mensagemId, indice, fields, true);
                return;
            }
            if (!response.ok || !result.sucesso) throw new Error(result.mensagem || "Não foi possível confirmar o lançamento.");
            await carregarHistorico();
            definirStatus("Despesa cadastrada.");
        } catch (error) {
            adicionarMensagemAssistente(String(error.message ?? error));
        } finally {
            button.disabled = false;
        }
    }

    function coletarConfirmacao(fields, indice) {
        const valor = name => fields.querySelector(`[name="${name}"]`)?.value ?? "";
        const valorTexto = valor("valorParcela");
        return {
            indice,
            titulo: valor("titulo"),
            descricao: valor("descricao"),
            valorParcela: Number.isFinite(Number(valorTexto)) && valorTexto !== "" ? Number(valorTexto) : null,
            vencimento: valor("vencimento") || null,
            quantidadeParcelas: Number.parseInt(valor("quantidadeParcelas"), 10) || 0,
            recorrente: Boolean(fields.querySelector('[name="recorrente"]')?.checked),
            pago: Boolean(fields.querySelector('[name="pago"]')?.checked),
            tag: valor("tag") || null,
            usuariosIds: Array.from(fields.querySelectorAll('input[name="usuariosIds"]:checked'), control => control.value)
        };
    }

    async function confirmarLote(button, mensagemId, artigo, ignorarPossiveisDuplicidades = false) {
        const cards = Array.from(artigo.querySelectorAll(".chat-draft-card:not(.is-confirmed)"));
        const confirmacoes = cards.map(card => coletarConfirmacao(card.querySelector(".chat-draft-fields"), Number(card.dataset.draftIndex)));
        const formData = new FormData();
        formData.append("__RequestVerificationToken", csrfToken);
        formData.append("mensagemId", String(mensagemId));
        formData.append("rascunhosJson", JSON.stringify(confirmacoes));
        formData.append("ignorarPossiveisDuplicidades", ignorarPossiveisDuplicidades ? "true" : "false");
        button.disabled = true;

        try {
            const response = await fetch(widget.dataset.confirmBatchUrl, { method: "POST", body: formData, credentials: "same-origin" });
            const result = await response.json();
            if (!response.ok && result.erro === "possivel_duplicidade_lote") {
                const indices = (result.indicesComDuplicidade ?? []).map(indice => indice + 1).join(", ");
                const detalhe = indices ? `\n\nLançamentos para revisar: ${indices}.` : "";
                if (window.confirm(`${result.mensagem}${detalhe}\n\nDeseja confirmar o lote mesmo assim?`))
                    return confirmarLote(button, mensagemId, artigo, true);
                return;
            }
            if (!response.ok || !result.sucesso) throw new Error(result.mensagem || "Não foi possível confirmar todas as despesas.");
            await carregarHistorico();
            definirStatus("Todas as despesas foram cadastradas.");
        } catch (error) {
            adicionarMensagemAssistente(String(error.message ?? error));
        } finally {
            button.disabled = false;
        }
    }

    function renderizarMensagem(mensagem) {
        const artigo = criarElemento("article", `chat-message ${mensagem.papel === "user" ? "chat-message-user" : "chat-message-assistant"}`);
        artigo.dataset.messageId = String(mensagem.id);
        artigo.append(criarElemento("div", "chat-bubble", mensagem.conteudo));
        if (mensagem.consulta) renderizarTabelaConsulta(artigo, mensagem.consulta);
        if (mensagem.rascunhos?.length) {
            mensagem.rascunhos.forEach(rascunho => artigo.append(criarRascunho(mensagem.id, rascunho)));
            const pendentes = mensagem.rascunhos.filter(rascunho => !rascunho.confirmado);
            if (pendentes.length > 1) {
                const acoesLote = criarElemento("div", "chat-draft-bulk-actions");
                const botaoLote = criarElemento("button", "chat-draft-confirm chat-draft-confirm-all", `Confirmar todas as ${pendentes.length} despesas`);
                botaoLote.type = "button";
                botaoLote.addEventListener("click", () => confirmarLote(botaoLote, mensagem.id, artigo));
                acoesLote.append(botaoLote);
                artigo.append(acoesLote);
            }
        }
        if (mensagem.edicoes?.length)
            mensagem.edicoes.forEach(edicao => artigo.append(criarEdicao(mensagem.id, edicao)));
        messages.append(artigo);
    }

    function adicionarMensagemAssistente(texto) {
        const artigo = criarElemento("article", "chat-message chat-message-assistant");
        artigo.append(criarElemento("div", "chat-bubble", texto));
        messages.append(artigo);
        irParaOFim();
    }

    function definirStatus(texto) {
        status.textContent = texto;
    }

    async function carregarHistorico() {
        try {
            const response = await fetch(widget.dataset.loadUrl, { credentials: "same-origin" });
            if (!response.ok) throw new Error("Não foi possível carregar o histórico.");
            const result = await response.json();
            state.opcoes = result.opcoes;
            messages.replaceChildren();
            if (!result.mensagens?.length) {
                adicionarMensagemAssistente("Olá! Posso consultar seus gastos, comparar períodos ou preparar um lançamento a partir de uma mensagem ou imagem. Vou mostrar os dados para você revisar antes de gravar.");
            } else {
                result.mensagens.forEach(renderizarMensagem);
            }
            definirStatus("Histórico sincronizado · processamento local");
            irParaOFim();
        } catch (error) {
            definirStatus(String(error.message ?? error));
        }
    }

    function mostrarIndicadorPensando() {
        const indicador = criarElemento("article", "chat-message chat-message-assistant chat-thinking");
        indicador.dataset.thinking = "true";
        indicador.append(criarElemento("div", "chat-bubble", "Analisando localmente…"));
        messages.append(indicador);
        irParaOFim();
    }

    async function enviarMensagem(event) {
        event.preventDefault();
        if (state.busy) return;
        const texto = input.value.trim();
        const arquivo = imageInput.files?.[0];
        if (!texto && !arquivo) return;
        if (arquivo && arquivo.size > 5 * 1024 * 1024) {
            definirStatus("A imagem deve ter até 5 MB.");
            return;
        }

        state.busy = true;
        sendButton.disabled = true;
        adicionarMensagemUsuario(texto, arquivo?.name);
        input.value = "";
        imageInput.value = "";
        fileName.textContent = "";
        mostrarIndicadorPensando();
        definirStatus("O Ollama local está processando a mensagem.");

        const formData = new FormData();
        formData.append("__RequestVerificationToken", csrfToken);
        formData.append("mensagem", texto);
        if (arquivo) formData.append("imagem", arquivo);

        try {
            const response = await fetch(widget.dataset.sendUrl, { method: "POST", body: formData, credentials: "same-origin" });
            const result = await response.json();
            messages.querySelector('[data-thinking="true"]')?.remove();
            if (!response.ok || !result.sucesso) throw new Error(result.mensagem || "Não foi possível processar a mensagem.");
            if (result.resposta) renderizarMensagem(result.resposta);
            definirStatus("Processado localmente · confirme um rascunho antes de gravar");
            irParaOFim();
        } catch (error) {
            messages.querySelector('[data-thinking="true"]')?.remove();
            adicionarMensagemAssistente(String(error.message ?? error));
            definirStatus("O envio não foi concluído.");
        } finally {
            state.busy = false;
            sendButton.disabled = false;
            input.focus();
        }
    }

    async function limparHistorico() {
        if (!window.confirm("Apagar as mensagens e os rascunhos salvos neste histórico?")) return;
        const formData = new FormData();
        formData.append("__RequestVerificationToken", csrfToken);
        try {
            const response = await fetch(widget.dataset.clearUrl, { method: "POST", body: formData, credentials: "same-origin" });
            if (!response.ok) throw new Error("Não foi possível limpar o histórico.");
            await carregarHistorico();
            definirStatus("Histórico limpo.");
        } catch (error) {
            definirStatus(String(error.message ?? error));
        }
    }

    document.getElementById("chat-minimize").addEventListener("click", () => setOpen(false));
    launcher.addEventListener("click", () => setOpen(true, true));
    document.getElementById("chat-clear").addEventListener("click", limparHistorico);
    form.addEventListener("submit", enviarMensagem);
    input.addEventListener("keydown", event => {
        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            form.requestSubmit();
        }
    });
    imageInput.addEventListener("change", () => {
        fileName.textContent = imageInput.files?.[0]?.name ?? "";
    });

    setOpen(localStorage.getItem(openStorageKey) !== "false");
    carregarHistorico();
})();
