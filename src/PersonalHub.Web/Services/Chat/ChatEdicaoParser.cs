using System.Globalization;
using System.Text.RegularExpressions;

namespace PersonalHub.Web.Services.Chat;

public static class ChatEdicaoParser
{
    private static readonly Regex AlvoDespesaEntreAspas = new(
        @"\bdespesa\s+(?:(?:identificada|identificado)\s+como\s+|(?:chamada|chamado|de\s+nome|de\s+t[i\u00ed]tulo|com\s+t[i\u00ed]tulo)\s+)?[""“‘](?<alvo>[^""“”‘’]+)[""”’]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TituloDaDespesaParaNovoTitulo = new(
        @"\bt[i\u00ed]tulo\s+da\s+despesa\s+(?<atual>.+?)\s+para\s+(?<novo>[^.;\r\n]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VerboEdicao = new(
        @"\b(?:edite|editar|altere|alterar|mude|mudar|modifique|modificar|atualize|atualizar|corrija|corrigir)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AlvoDespesa = new(
        @"\bdespesa\s+(?:(?:identificada|identificado)\s+como\s+|(?:chamada|chamado|de\s+nome|de\s+t[i\u00ed]tulo|com\s+t[i\u00ed]tulo)\s+)?(?<alvo>[^.;\r\n]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TentarInterpretar(string mensagem, out ChatEdicaoIntent intent)
    {
        intent = new ChatEdicaoIntent();
        if (!VerboEdicao.IsMatch(mensagem))
            return false;

        Match alvo = AlvoDespesa.Match(mensagem);
        if (!alvo.Success)
            return false;

        string textoBusca = Regex.Replace(
            alvo.Groups["alvo"].Value,
            @"\s+(?:(?:e\s+)?(?:mude|altere|edite|defina|atualize|corrija|modifique|seja\s+alterad[oa])|para\s+(?:o\s+)?(?:valor|descri[cç][aã]o|t[ií]tulo|nome|vencimento|data|tag|parcelas))\b.*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Match alvoEntreAspas = AlvoDespesaEntreAspas.Match(mensagem);
        if (alvoEntreAspas.Success)
            textoBusca = alvoEntreAspas.Groups["alvo"].Value;
        else
            textoBusca = Regex.Replace(
                textoBusca,
                @"\s+para\s+(?:R\$\s*)?\d[\d.,]*\s*\u20ac?.*$",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        textoBusca = LimparExtremidades(textoBusca);
        if (textoBusca.Length < 2)
            return false;

        intent.TextoBusca = textoBusca[..Math.Min(textoBusca.Length, 100)];
        intent.Titulo = CapturarCampo(mensagem, @"(?:t[ií]tulo|nome)\s+(?:para|como|deve\s+ser)\s+(?<valor>[^.;\r\n]+)");
        intent.Descricao = CapturarCampo(mensagem, @"(?:descri[cç][aã]o|descritivo)\s+(?:para|como|deve\s+ser)\s+(?<valor>[^.;\r\n]+)");

        string? valorTexto = CapturarCampo(mensagem, @"(?:valor(?:\s+por\s+parcela)?|pre[cç]o)\s+(?:para|como|deve\s+ser|de)\s+(?<valor>R\$\s*)?(?<numero>\d[\d.,]*)", "numero");
        valorTexto ??= CapturarCampo(mensagem, @"(?:valor(?:\s+por\s+parcela)?|pre[c\u00e7]o)\b(?:(?![.;\r\n]).)*?\bpara\s+(?:R\$\s*)?(?<numero>\d[\d.,]*)\s*€?", "numero");
        if (valorTexto != null && TentarLerValor(valorTexto, out decimal valor))
            intent.ValorParcela = valor;

        string? dataTexto = CapturarCampo(mensagem, @"(?:vencimento|data)\s+(?:para|em|como)\s+(?<valor>\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4})");
        if (DateOnly.TryParseExact(dataTexto, new[] { "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly vencimento))
            intent.Vencimento = vencimento;

        string? quantidade = CapturarCampo(mensagem, @"(?:quantidade\s+de\s+parcelas|n[uú]mero\s+de\s+parcelas|parcelas)\s+(?:para|como|de)\s+(?<valor>\d+)");
        if (int.TryParse(quantidade, NumberStyles.None, CultureInfo.InvariantCulture, out int parcelas))
            intent.QuantidadeParcelas = parcelas;

        MatchCollection tags = Regex.Matches(
            mensagem,
            @"(?:tags?|categorias?)\s+(?:para|como|chamada|chamado|deve\s+ser)\s+(?<valor>[^.;\r\n]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (tags.Count > 0)
        {
            string valorTags = LimparExtremidades(tags[^1].Groups["valor"].Value);
            intent.Tags = valorTags.Contains("sem tag", StringComparison.OrdinalIgnoreCase) ||
                          valorTags.Contains("remova", StringComparison.OrdinalIgnoreCase) ||
                          valorTags.Contains("nenhuma", StringComparison.OrdinalIgnoreCase)
                ? new List<string>()
                : valorTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        else if (Regex.IsMatch(mensagem, @"\b(?:remova|limpe|apague)\s+(?:todas\s+as\s+)?tags?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            intent.Tags = new List<string>();
        }

        Match tituloDaDespesa = TituloDaDespesaParaNovoTitulo.Match(mensagem);
        if (tituloDaDespesa.Success)
        {
            textoBusca = LimparExtremidades(tituloDaDespesa.Groups["atual"].Value);
            intent.TextoBusca = textoBusca[..Math.Min(textoBusca.Length, 100)];
            intent.Titulo = LimparExtremidades(tituloDaDespesa.Groups["novo"].Value);
        }

        return true;
    }

    private static string? CapturarCampo(string mensagem, string padrao, string grupo = "valor")
    {
        Match match = Regex.Match(mensagem, padrao, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? LimparExtremidades(match.Groups[grupo].Value) : null;
    }

    private static bool TentarLerValor(string texto, out decimal valor)
    {
        string limpo = texto.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(" ", string.Empty);
        if (limpo.Contains(','))
            limpo = limpo.Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(limpo, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out valor);
    }

    private static string LimparExtremidades(string texto) =>
        texto.Trim().Trim('"', '\'', '“', '”', '‘', '’', ',', ';', '.', ':');
}
