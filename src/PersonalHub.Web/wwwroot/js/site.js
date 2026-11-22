const themeStorageKey = "personalhub.theme";

function obterTemaAtual() {
    const temaSalvo = localStorage.getItem(themeStorageKey);
    return temaSalvo === "light" ? "light" : "dark";
}

function aplicarTema(tema) {
    const temaNormalizado = tema === "dark" ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", temaNormalizado);
    localStorage.setItem(themeStorageKey, temaNormalizado);

    const botaoTema = document.getElementById("theme-toggle");
    if (!botaoTema) return;

    const proximoTema = temaNormalizado === "dark" ? "light" : "dark";
    botaoTema.setAttribute("aria-label", `Alternar para ${proximoTema} mode`);
    botaoTema.setAttribute("title", `Alternar para ${proximoTema} mode`);
    botaoTema.setAttribute("aria-pressed", (temaNormalizado === "dark").toString());
}

window.exibirToastRetorno = function (opcoes) {
    return Swal.fire({
        position: "top-start",
        timer: 3000,
        toast: true,
        showConfirmButton: false,
        ...opcoes
    });
};

document.addEventListener("DOMContentLoaded", function () {
    aplicarTema(obterTemaAtual());

    const botaoTema = document.getElementById("theme-toggle");
    if (!botaoTema) return;

    botaoTema.addEventListener("click", function () {
        const novoTema = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
        aplicarTema(novoTema);
    });
});

/*
 * Localized default methods for the jQuery validation plugin.
 * Locale: PT_BR
 */
//jQuery.extend(jQuery.validator.methods, {
//    date: function (value, element) {
//        return this.optional(element) || /^\d\d?\/\d\d?\/\d\d\d?\d?$/.test(value);
//    },
//    number: function (value, element) {
//        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
//    }
//});
