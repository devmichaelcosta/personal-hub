using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PersonalHub.Web.Services.Chat;

public static class ChatConsultaParser
{
    private static readonly string[] Meses =
    {
        "janeiro", "fevereiro", "marco", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
    };

    public static bool TentarInterpretarTotalMensal(string? texto, out ChatConsultaIntent consulta)
    {
        consulta = new ChatConsultaIntent();
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        string normalizado = RemoverAcentos(texto).ToLowerInvariant();
        bool pedidoDeConsulta = Regex.IsMatch(normalizado, @"\b(quanto|total|soma|relatorio|mostre|informe|consulte|liste|quais|como)\b");
        bool mencionaDespesas = Regex.IsMatch(normalizado, @"\b(gastos?|despesas?|gastei)\b");
        bool pedeAlteracao = Regex.IsMatch(normalizado, @"\b(registre|registrar|cadastre|cadastrar|lance|lancar|adicione|adicionar|crie|criar|comprei|edite|editar|altere|alterar|mude|mudar|atualize|atualizar)\b");
        bool comparaPeriodos = Regex.IsMatch(normalizado, @"\b(compar\w*|versus|vs|diferenca)\b");
        bool informaDatasExatas = Regex.IsMatch(normalizado, @"\b(?:\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?|\d{4}-\d{2}-\d{2})\b");
        if (!pedidoDeConsulta || !mencionaDespesas || pedeAlteracao || comparaPeriodos || informaDatasExatas)
            return false;

        DateOnly hoje = DateOnly.FromDateTime(DateTime.Today);
        Match mesEncontrado = Regex.Match(normalizado, @"\b(janeiro|fevereiro|marco|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro)\b");
        Match anoEncontrado = Regex.Match(normalizado, @"\b(20\d{2})\b");

        DateOnly inicio;
        if (mesEncontrado.Success)
        {
            int numeroMes = Array.IndexOf(Meses, mesEncontrado.Value) + 1;
            int ano = anoEncontrado.Success ? int.Parse(anoEncontrado.Value, CultureInfo.InvariantCulture) : hoje.Year;
            inicio = new DateOnly(ano, numeroMes, 1);
        }
        else if (Regex.IsMatch(normalizado, @"\b(mes passado|mes anterior)\b"))
        {
            DateOnly mesAtual = new(hoje.Year, hoje.Month, 1);
            inicio = mesAtual.AddMonths(-1);
        }
        else if (Regex.IsMatch(normalizado, @"\b(proximo mes|mes que vem)\b"))
        {
            inicio = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(1);
        }
        else
        {
            inicio = new DateOnly(hoje.Year, hoje.Month, 1);
        }

        consulta.Inicio = inicio;
        consulta.Fim = new DateOnly(inicio.Year, inicio.Month, DateTime.DaysInMonth(inicio.Year, inicio.Month));
        consulta.Situacao = Regex.IsMatch(normalizado, @"\b(pendentes?|a vencer)\b")
            ? "pendentes"
            : Regex.IsMatch(normalizado, @"\b(em aberto|a pagar|nao pagas?)\b")
                ? "abertas"
            : Regex.IsMatch(normalizado, @"\b(vencidas?|atrasadas?)\b")
                ? "vencidas"
                : Regex.IsMatch(normalizado, @"\b(pagas?)\b")
                    ? "pagas"
                    : "todas";
        consulta.AgruparPor = Regex.IsMatch(normalizado, @"\b(por tag|por categoria)\b") ? "tag" : "nenhum";
        return true;
    }

    private static string RemoverAcentos(string texto)
    {
        string decomposto = texto.Normalize(NormalizationForm.FormD);
        StringBuilder resultado = new(decomposto.Length);
        foreach (char caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                resultado.Append(caractere);
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
