using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PersonalHub.Domain.Entidades;

namespace PersonalHub.Web.Services.Chat;

public static class ChatEdicaoSnapshot
{
    public static string Assinar(Despesa despesa, IEnumerable<OcorrenciaDespesaRecorrente> ocorrencias)
    {
        string conteudo = JsonSerializer.Serialize(new
        {
            despesa.Id,
            despesa.Titulo,
            despesa.Descritivo,
            despesa.Valor,
            despesa.QtdParcelas,
            Vencimento = despesa.Vencimento.ToString("O", CultureInfo.InvariantCulture),
            despesa.Recorrente,
            Tags = despesa.TagsDespesas
                .Select(item => item.Tag?.Nome ?? item.TagId.ToString(CultureInfo.InvariantCulture))
                .OrderBy(nome => nome, StringComparer.Ordinal)
                .ToArray(),
            Participantes = despesa.UsuariosEnvolvidos
                .Select(item => item.UsuarioId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray(),
            Parcelas = despesa.Parcelas
                .OrderBy(item => item.Numero)
                .Select(item => new
                {
                    item.Id,
                    item.Numero,
                    item.Valor,
                    Vencimento = item.Vencimento.ToString("O", CultureInfo.InvariantCulture),
                    item.Pago
                })
                .ToArray(),
            Ocorrencias = ocorrencias
                .OrderBy(item => item.MesReferencia)
                .Select(item => new
                {
                    item.Id,
                    MesReferencia = item.MesReferencia.ToString("O", CultureInfo.InvariantCulture),
                    Vencimento = item.Vencimento.ToString("O", CultureInfo.InvariantCulture),
                    item.Valor,
                    item.Pago
                })
                .ToArray()
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(conteudo)));
    }
}
