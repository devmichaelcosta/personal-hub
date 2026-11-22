$.validator.methods.range = function (value, element, param) {
    var globalizedValue = value.replace(",", ".");
    return this.optional(element) || (globalizedValue >= param[0] && globalizedValue <= param[1]);
};

//$.validator.methods.number = function (value, element) {
//    return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:[\s\.,]\d{3})+)(?:[\.,]\d+)?$/.test(value);
//};

$.validator.methods.number = function (value, element) {
    return this.optional(element) || /-?(?:\d+|\d{1,3}(?:[\s\.,]\d{3})+)(?:[\.,]\d+)?$/.test(value);
};

//$.validator.methods.date = function (value, element) {
//    var date = value.split("/");
//    return this.optional(element) || !/Invalid|NaN/.test(new Date(date[2], date[1], date[0]).toString());
//};

$.validator.addMethod("greaterThanZero", function (value, element) {
    var valorPtBr = Number(value.replace(/\./g, "").replace(",", "."));
    return this.optional(element) || (Number.isFinite(valorPtBr) && valorPtBr > 0);

}, "Deve ser maior que 0");
