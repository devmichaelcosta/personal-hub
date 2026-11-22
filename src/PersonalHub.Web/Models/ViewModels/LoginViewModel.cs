using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o campo {0}"), Display(Name = "Usuário")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o campo {0}"), Display(Name = "Senha")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Lembrar acesso")]
        public bool RememberMe { get; set; }
    }
}
