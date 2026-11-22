namespace PersonalHub.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class CustomErrorViewModel
    {
        public string Mensagem { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public int ErrorCode { get; set; }

        public CustomErrorViewModel(int? errorCode)
        {
            if (errorCode == 500)
            {
                Mensagem = "Ocorreu um erro tente novamente mais tarde.";
                Titulo = "Ops, ocorreu um erro!";
                ErrorCode = 500;
            }
            else if (errorCode == 404)
            {
                Mensagem = "A página que você solicitou não existe.";
                Titulo = "Ops, página não encontrada!";
                ErrorCode = 500;
            }
            else if (errorCode == 403)
            {
                Mensagem = "Você não tem permissão para estar aqui.";
                Titulo = "Acesso negado!";
                ErrorCode = 500;
            }
        }
    }
}
