import Despesa from "/js/despesa/despesa.js";

let despesasModulo = null;

$(function () {
    despesasModulo = new Despesa();
    despesasModulo.iniciarEventos();
});