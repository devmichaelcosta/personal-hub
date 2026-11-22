import Despesa from "./despesa/despesa.js";

class Rendimento
{
    iniciarEventos() {
        let formCadastroRendimentoEl = $("#form-cadastro-rendimento");
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse(formCadastroRendimentoEl);
        }

        $("#btn-cadastrar-rendimento-modal").unbind('click');
        $("#btn-cadastrar-rendimento-modal").click(function () {
            let isValid = formCadastroRendimentoEl.validate().form();

            if (isValid) {
                $.post("/Rendimentos/Cadastrar", formCadastroRendimentoEl.serialize(), function (data) {
                    if (data.sucesso) {
                        despesasModulo.listarDespesasDashboard();
                        $.sidebar.fnFechar();

                        window.exibirToastRetorno({
                            text: 'Rendimento cadastrado com sucesso.',
                            icon: 'success'
                        });
                    }
                });
            }

            return false;
        });
    }
}

export default class Dashboard
{
    constructor() {
        this.iniciarEventos();
    }

    iniciarEventos() {
        console.log("Dashboard.iniciarEventos");
        this.iniciarSeletorUsuarios();
        this.iniciarAtualizacaoFiltros();
        this.iniciarOrdenacaoTabela();
        this.iniciarModalRendimentos();

        $("#dashboard-filtros").off("submit.dashboard").on("submit.dashboard", function (ev) {
            ev.preventDefault();
            despesasModulo.listarDespesasDashboard();
        });

        let checboxMarcarComoPagoEl = $("input[name=Pago]");
        checboxMarcarComoPagoEl.unbind('change');
        checboxMarcarComoPagoEl.change(function () {
            let pago = $(this).is(":checked");
            let parcelaId = $(this).data('parcelaId');
            let ocorrenciaId = $(this).data('ocorrenciaId');
            $.post("/Parcelas/MarcarComoPaga", { pago: pago, parcelaId: parcelaId, ocorrenciaId: ocorrenciaId }, function () {
                window.exibirToastRetorno({
                    text: 'Despesa atualizada com sucesso.',
                    icon: 'success'
                });
                despesasModulo.listarDespesasDashboard();
            });
        });

        $("#tb-despesas tbody tr").unbind('click');
        $("#tb-despesas tbody tr").click(function (ev) {
            const alvoInterativo = $(ev.target).closest("a, button, input, label, select, textarea, .btn").length > 0;
            if (alvoInterativo) {
                return;
            }

            const checkbox = $(this).find("input[name=Pago]").first();
            if (!checkbox.length) {
                return;
            }

            checkbox.prop("checked", !checkbox.is(":checked"));
            checkbox.trigger("change");
        });

        $(".btn-editar").unbind('click');
        $('.btn-editar').click(function () {
            let action = $(this).data('actionEditar');
            $.sidebar(this, {
                url: action,
                callbackAbrir: function () {
                    despesasModulo.iniciarEventos();
                }
            });
        });

        $("#btn-cadastrar, #btn-cadastrar-topo").unbind('click');
        $("#btn-cadastrar, #btn-cadastrar-topo").click(function () {
            let action = $(this).data('action');
            $.sidebar(this, {
                url: action,
                callbackAbrir: function () {
                    despesasModulo.iniciarEventos();
                }
            });
        });

        $("#btn-cadastrar-rendimento").unbind('click');
        $("#btn-cadastrar-rendimento").click(function () {
            let action = $(this).data('action');
            $.sidebar(this, {
                url: action,
                callbackAbrir: function () {
                    rendimentoModulo.iniciarEventos();
                }
            });
        });

        $('.btn-deletar').unbind('click');
        $('.btn-deletar').click(function (ev) {
            let el = $(this);
            ev.preventDefault();

            Swal.fire({
                title: "Deseja remover a despesa?",
                text: el.data('recorrente') === true
                    ? "Esta despesa é recorrente. Todas as ocorrências mensais, inclusive as pagas, serão excluídas e novos lançamentos não serão gerados. Esta ação não pode ser desfeita."
                    : undefined,
                showCancelButton: true,
                confirmButtonText: "Remover",
                cancelButtonText: "Cancelar",
                icon: "warning",
                confirmButtonColor: "#212529",
                cancelButtonColor: "#dc3545"

            }).then((result) => {
                if (result.isConfirmed) {
                    $.post(el.data('action'), function (data) {
                        if (data.success) {
                            window.exibirToastRetorno({
                                text: 'Despesa removida com sucesso.',
                                icon: 'success'
                            });
                            despesasModulo.listarDespesasDashboard();
                        }

                    }).fail(function () {
                        console.error("Não foi possível remover a despesa.");
                    });
                }
            });

            return false;
            });
    }

    iniciarAtualizacaoFiltros() {
        const form = $("#dashboard-filtros");
        if (!form.length) {
            return;
        }

        let timeoutAtualizacao;
        const atualizarDashboard = function () {
            window.clearTimeout(timeoutAtualizacao);
            timeoutAtualizacao = window.setTimeout(function () {
                despesasModulo.listarDespesasDashboard();
            }, 300);
        };

        form.find("#Pesquisa")
            .off("input.dashboardFilters")
            .on("input.dashboardFilters", atualizarDashboard);

        const mes = form.find("#MesReferenciaMes");
        const ano = form.find("#MesReferenciaAno");
        const valorMes = form.find("#MesReferencia");
        ano.prop("disabled", !mes.val());
        const atualizarMes = function () {
            ano.prop("disabled", !mes.val());
            valorMes.val(mes.val() ? `${ano.val()}-${mes.val()}` : "");
            const subtitulo = document.querySelector(".dashboard-title p");
            if (subtitulo) {
                subtitulo.textContent = mes.val()
                    ? `${mes.find(":selected").text()} ${ano.val()}`
                    : "Todos os meses";
            }
            despesasModulo.listarDespesasDashboard();
        };

        mes.add(ano).off("change.dashboardFilters").on("change.dashboardFilters", atualizarMes);
    }

    iniciarSeletorUsuarios() {
        const pickers = $(".dashboard-multi-picker");
        if (!pickers.length) {
            return;
        }

        pickers.each(function () {
            const picker = $(this);
            const toggle = picker.find(".dashboard-user-picker-toggle");
            const toggleText = picker.find(".dashboard-user-picker-text");
            const checkboxes = picker.find(".dashboard-multi-checkbox");
            const hiddenContainer = picker.find(".dashboard-multi-hidden");

            const atualizarEstado = function () {
                const selecionados = checkboxes.filter(":checked");

                hiddenContainer.empty();
                selecionados.each(function () {
                    $("<input>", {
                        type: "hidden",
                        name: $(this).data("name"),
                        value: $(this).val()
                    }).appendTo(hiddenContainer);
                });

                if (selecionados.length === 0) {
                    toggleText.text(picker.data("emptyText"));
                } else if (selecionados.length === checkboxes.length) {
                    toggleText.text(picker.data("allText"));
                } else if (selecionados.length === 1) {
                    toggleText.text(selecionados.first().data("label"));
                } else {
                    toggleText.text(`${selecionados.length} ${picker.data("manyText")}`);
                }
            };

            toggle.off("click.userPicker").on("click.userPicker", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();

                pickers.not(picker).removeClass("is-open")
                    .find(".dashboard-user-picker-toggle")
                    .attr("aria-expanded", "false");

                const aberto = picker.toggleClass("is-open").hasClass("is-open");
                toggle.attr("aria-expanded", aberto.toString());
            });

            picker.find(".dashboard-user-picker-menu")
                .off("click.userPicker")
                .on("click.userPicker", function (ev) {
                    ev.stopPropagation();
                });

            checkboxes.off("change.userPicker").on("change.userPicker", function () {
                atualizarEstado();
                despesasModulo.listarDespesasDashboard();
            });

            picker.find("[data-user-picker-action]")
                .off("click.userPicker")
                .on("click.userPicker", function () {
                    const selecionarTodos = $(this).data("userPickerAction") === "all";
                    checkboxes.prop("checked", selecionarTodos);
                    atualizarEstado();
                    despesasModulo.listarDespesasDashboard();
                });

            atualizarEstado();
        });

        $(document)
            .off("click.userPicker")
            .on("click.userPicker", function () {
                pickers.removeClass("is-open");
                pickers.find(".dashboard-user-picker-toggle").attr("aria-expanded", "false");
            });
    }

    iniciarOrdenacaoTabela() {
        const tabela = $("#tb-despesas");
        if (!tabela.length) {
            return;
        }

        tabela.find("thead th[data-sort-key]")
            .off("click.dashboardSort")
            .on("click.dashboardSort", function () {
                const cabecalho = $(this);
                const indiceColuna = cabecalho.index();
                const ordemAtual = cabecalho.attr("data-sort-order") || "none";
                const novaOrdem = ordemAtual === "asc" ? "desc" : "asc";
                const linhas = tabela.find("tbody tr").get();

                tabela.find("thead th[data-sort-key]")
                    .removeAttr("data-sort-order")
                    .removeAttr("aria-sort");
                cabecalho.attr("data-sort-order", novaOrdem).attr("aria-sort", novaOrdem === "asc" ? "ascending" : "descending");

                linhas.sort(function (linhaA, linhaB) {
                    const valorA = $(linhaA).children().eq(indiceColuna).data("sortValue")?.toString() || "";
                    const valorB = $(linhaB).children().eq(indiceColuna).data("sortValue")?.toString() || "";
                    const chaveOrdenacao = cabecalho.data("sortKey");
                    const comparacao = chaveOrdenacao === "valor"
                        ? Number(valorA) - Number(valorB)
                        : chaveOrdenacao === "data"
                            ? valorA.split("/").reverse().join("").localeCompare(valorB.split("/").reverse().join(""))
                        : valorA.localeCompare(valorB, "pt-BR", { numeric: true, sensitivity: "base" });

                    return novaOrdem === "asc" ? comparacao : -comparacao;
                });

                tabela.children("tbody").append(linhas);
            });
    }

    iniciarModalRendimentos() {
        const widget = $(".indicador-rendimentos");
        if (!widget.length) {
            return;
        }

        const dashboard = this;
        const abrirModal = function (ev) {
            if (ev.type === "keydown" && ev.key !== "Enter" && ev.key !== " ") {
                return;
            }

            ev.preventDefault();
            const filtros = $("#dashboard-filtros").serialize();
            $.sidebar(this, {
                url: "/Home/RendimentosDashboard?" + filtros,
                callbackAbrir: () => dashboard.iniciarTabelaRendimentos()
            });
        };

        widget
            .off("click.dashboardRendimentos keydown.dashboardRendimentos")
            .on("click.dashboardRendimentos keydown.dashboardRendimentos", abrirModal);
    }

    iniciarTabelaRendimentos() {
        const tabela = $("#tb-rendimentos-dashboard");
        if (!tabela.length || !$.fn.DataTable || $.fn.DataTable.isDataTable(tabela)) {
            return;
        }

        tabela.DataTable({
            pageLength: 10,
            lengthMenu: [10, 25, 50],
            order: [[0, "desc"]],
            language: {
                emptyTable: "Nenhum registro encontrado",
                info: "Mostrando _START_ até _END_ de _TOTAL_ registros",
                infoEmpty: "Mostrando 0 até 0 de 0 registros",
                infoFiltered: "(filtrado de _MAX_ registros no total)",
                lengthMenu: "Exibir _MENU_ registros",
                loadingRecords: "Carregando...",
                processing: "Carregando...",
                search: "Buscar",
                zeroRecords: "Nenhum registro encontrado",
                paginate: {
                    first: "Primeiro",
                    last: "Último",
                    next: "Próximo",
                    previous: "Anterior"
                }
            },
            columnDefs: [
                { targets: 4, className: "text-end text-nowrap" }
            ]
        });
    }
}

window.dashboardModulo = new Dashboard();
window.despesasModulo = new Despesa();
window.rendimentoModulo = new Rendimento();
