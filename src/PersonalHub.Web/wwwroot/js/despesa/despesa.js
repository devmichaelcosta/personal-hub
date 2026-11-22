import Tags from "/lib/bootstrap5-tags/tags.min.js";

export default class Despesa
{
    constructor() {
        
    }

    iniciarEventos() {
        console.log("Despesa.iniciarEventos");

        let qtdParcelasEl = $("#QtdParcelas");
        let valorEl = $("#ValorParcela");
        let totalEl = $("#Total");

        valorEl
            .off("input.valorPtBr")
            .on("input.valorPtBr", function () {
                let valor = this.value
                    .replace(/\./g, ",")
                    .replace(/[^0-9,]/g, "");
                const primeiraVirgula = valor.indexOf(",");

                if (primeiraVirgula >= 0) {
                    valor = valor.substring(0, primeiraVirgula + 1)
                        + valor.substring(primeiraVirgula + 1).replace(/,/g, "");
                }

                if (this.value !== valor) {
                    this.value = valor;
                }
            });

        const atualizarTipoCadastro = function () {
            const tipo = parseInt($("[name=Tipo]:checked").val(), 10);
            const parcelado = tipo === 0;
            const gerarVarias = tipo === 1;
            const recorrente = tipo === 2;

            if (parcelado) {
                $("#campos-tipo-gerar-varias").addClass("visually-hidden");
                qtdParcelasEl.val(1).prop("disabled", false);
            } else if (gerarVarias) {
                $("#campos-tipo-gerar-varias").removeClass("visually-hidden");
                qtdParcelasEl.val(1).prop("disabled", true);
            } else if (recorrente) {
                $("#campos-tipo-gerar-varias").addClass("visually-hidden");
                qtdParcelasEl.val(0).prop("disabled", true);
            }

            $("#campo-qtd-parcelas, #campo-total-despesa").toggleClass("visually-hidden", recorrente);
            $("#label-valor-despesa").text(parcelado ? "Valor parcela" : "Valor mensal");
        };

        $("[name=Tipo]").off("change.tipoCadastro").on("change.tipoCadastro", atualizarTipoCadastro);
        atualizarTipoCadastro();

        $(".campos-calculadores-total")
            .off("input.calculadorTotal change.calculadorTotal")
            .on("input.calculadorTotal change.calculadorTotal", function () {
            let qtd = parseFloat(String(qtdParcelasEl.val()).replace(",", "."));
            let valor = parseFloat(String(valorEl.val()).replace(",", "."));
            let existemNumerosInvalidos = isNaN(qtd) || isNaN(valor);

            if (!existemNumerosInvalidos) {
                let total = qtd * valor;
                totalEl.val(total);
            } else
                totalEl.val("0");
        });

        valorEl.trigger('change');
        valorEl.rules("add", { greaterThanZero: true });

        qtdParcelasEl.rules("add", { greaterThanZero: true });

        $("#UsuariosEnvolvidos").rules("add", {
            required: true,
            messages: {
                required: "Selecione ao menos um usuário"
            }
        });

        Tags.init("select[id=Tags]");

        $("#Tags").parent().find("input").blur(function () {
            var valorInformado = $("#Tags").parent().find("input").val();
            let podeAdicionar = valorInformado !== '' && !tagExiste(valorInformado)

            if (podeAdicionar) {
                let spanItem = $(`<span class="badge me-2 bg-primary" data-value="${valorInformado}"/>`);
                let buttonEl = $('<button type="button" style="font-size:0.65em" class="me-2 btn-close btn-close-white" aria-label="Clear"/>');
                buttonEl.click(function () {
                    let valor = $(this).parent().data('value');
                    removerSelectOptionTag(valor);
                    $(this).parent().remove();
                });
                spanItem.html(buttonEl);
                spanItem.append(valorInformado);

                let htmlOption = `<option value="${valorInformado}" data-value="${valorInformado}" selected="selected">${valorInformado}</option>`;

                $("#Tags").append(htmlOption);
                $(".form-control.dropdown > div input").val('');
                $(".form-control.dropdown > div input").before(spanItem);
            }
        });

        let btnCadastrarDespesaModal = $("#btn-cadastrar-despesa-modal");
        if (btnCadastrarDespesaModal.length > 0) {
            btnCadastrarDespesaModal.unbind('click');
            btnCadastrarDespesaModal.click(function () {
                let isValid = $("#form-cadastro").validate().form();
                if (isValid) {
                    $.post("/Despesas/Cadastrar", $("#form-cadastro").serialize(), function (data) {
                        if (data.sucesso) {
                            despesasModulo.listarDespesasDashboard();
                            $.sidebar.fnFechar();
                            window.exibirToastRetorno({
                                text: "Despesa cadastrada com sucesso.",
                                icon: "success"
                            });
                        }
                    });
                }

                return false;
            });
        }

        let btnAtualizarDespesaEl = $("#btn-atualizar-despesa");
        if (btnAtualizarDespesaEl.length > 0) {
            btnAtualizarDespesaEl.unbind('click');
            btnAtualizarDespesaEl.click(function () {
                let isValid = $("#form-dados-despesa").validate().form();
                if (isValid) {
                    $.post("/Despesas/EdicaoModal", $("#form-dados-despesa").serialize(), function (data) {
                        if (data.sucesso) {
                            despesasModulo.listarDespesasDashboard();
                            $.sidebar.fnFechar();
                        } else {
                            window.exibirToastRetorno({
                                text: data.erro || "Não foi possível atualizar a despesa.",
                                icon: "error"
                            });
                        }
                    });
                }

                return false;
            });
        }
    }

    listarDespesasDashboard() {
        const campoAtivo = document.activeElement;
        const restaurarBusca = campoAtivo && campoAtivo.id === "Pesquisa";
        const selecaoBusca = restaurarBusca
            ? {
                inicio: campoAtivo.selectionStart,
                fim: campoAtivo.selectionEnd
            }
            : null;
        let dados = $("#dashboard-filtros").serialize();

        $.get("/Home/ObterDashboardDetails", dados, function (dashboardDetailsHtml) {
            $("#dashboard-details").html(dashboardDetailsHtml);
            dashboardModulo.iniciarEventos();

            if (restaurarBusca) {
                const pesquisa = document.querySelector("#Pesquisa");
                pesquisa?.focus();

                if (pesquisa && selecaoBusca && typeof pesquisa.setSelectionRange === "function") {
                    pesquisa.setSelectionRange(selecaoBusca.inicio, selecaoBusca.fim);
                }
            }
        });
    }
}

function tagExiste(valor) {
    //console.log($(".badge[data-value='" + valor + "']"))
    return $(".badge[data-value='" + valor + "']").length > 0;
}

function removerSelectOptionTag(valor) {
    //console.log(valor)
    $("#Tags option[value='" + valor + "']").remove();
}
