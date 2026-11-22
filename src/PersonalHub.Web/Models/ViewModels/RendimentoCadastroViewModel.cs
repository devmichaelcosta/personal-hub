using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Web.Models.ViewModels
{
    public class RendimentoCadastroViewModel
    {
        [Required(ErrorMessage = "Informe o campo {0}")]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "Informe o campo {0}")]
        public decimal Valor { get; set; }
        
        public string? Descritivo { get; set; }

        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Usuário")]
        public string UsuarioId { get; set; } = string.Empty;

        public List<SelectListItem> UsuariosDisponiveis { get; set; }

        public RendimentoCadastroViewModel()
        {
            UsuariosDisponiveis = new List<SelectListItem>();
        }
    }
}
