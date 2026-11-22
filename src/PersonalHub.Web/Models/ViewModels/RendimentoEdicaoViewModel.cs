using PersonalHub.Domain.Entidades;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Web.Models.ViewModels
{
    public class RendimentoEdicaoViewModel : IRendimento
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Informe o campo {0}")]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "Informe o campo {0}")]
        public decimal Valor { get; set; }

        public string Descritivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Usuário")]
        public string UsuarioId { get; set; } = string.Empty;

        public List<SelectListItem> UsuariosDisponiveis { get; set; }

        public RendimentoEdicaoViewModel()
        {
            UsuariosDisponiveis = new List<SelectListItem>();
        }

        public RendimentoEdicaoViewModel(int id, DateTime data, decimal valor, string descritivo,
                string titulo, string usuarioId)
        {
            UsuariosDisponiveis = new List<SelectListItem>();
            this.Id = id;
            this.Data = data;
            this.Valor = valor;
            this.Descritivo = descritivo;
            this.Titulo = titulo;
            this.UsuarioId = usuarioId;
        }

        public static implicit operator RendimentoEdicaoViewModel(Rendimento rendimento)
        {
            return new RendimentoEdicaoViewModel(
                rendimento.Id,
                rendimento.Data, 
                rendimento.Valor,
                rendimento.Descritivo,
                rendimento.Titulo,
                rendimento.UsuarioId);
        }
    }
}
