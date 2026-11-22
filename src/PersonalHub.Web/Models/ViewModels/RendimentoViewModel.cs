using PersonalHub.Domain.Entidades;
using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Web.Models.ViewModels
{
    public class RendimentoViewModel
    {
        public RendimentoViewModel(int id, string titulo, string descritivo, decimal valor, DateTime data)
        {
            Id = id;
            Titulo = titulo;
            Descritivo = descritivo;
            Valor = valor.ToString("N2");
            Data = data.ToString("dd/MM/yyyy");
        }

        public int Id { get; set; }

        [Display(Name = "Título")]
        public string Titulo { get; set; }

        public string Descritivo { get; set; }
        public string Valor { get; set; }
        public string Data { get; set; }

        public static implicit operator RendimentoViewModel(Rendimento rendimento)
        {
            return new RendimentoViewModel(
                rendimento.Id,
                rendimento.Titulo,
                rendimento.Descritivo,
                rendimento.Valor,
                rendimento.Data);
        }
    }
}
