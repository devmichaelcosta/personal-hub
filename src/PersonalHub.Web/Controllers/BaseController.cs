using Microsoft.AspNetCore.Mvc;

namespace PersonalHub.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly string _apiPath = "https://localhost:7290";
        protected readonly string _usuarioAtual = string.Empty;

        public BaseController() 
        {
            
        }

        public void GerarErrosModelState()
        {
            var itensModelState = ModelState.Where(e => e.Value?.Errors.Count > 0).ToList();
            
            foreach (var itemModelState in itensModelState)
            {
                if (itemModelState.Value != null)
                    foreach (var item in itemModelState.Value.Errors)
                        ModelState.AddModelError(string.Empty, item.ErrorMessage);
            }
        }

        public string ObterUsuario()
        {
            var usuarioAtual = User?.Identity?.Name ?? string.Empty;
            return usuarioAtual;
        }
    }
}
