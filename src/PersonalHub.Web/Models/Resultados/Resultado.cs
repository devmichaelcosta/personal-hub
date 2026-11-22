namespace PersonalHub.Web.Models.Resultados
{
    public abstract class Resultado
    {
        public bool Sucesso { get; private set; }
        public string? Mensagem { get; private set; }
        public Resultado(bool sucesso)
        {
            this.Sucesso = sucesso;
        }
        public Resultado(bool sucesso,string mensagem)
        {
            this.Sucesso = sucesso;
            this.Mensagem = mensagem;   
        }
    }

    public class ResultadoSucesso : Resultado
    {
        public ResultadoSucesso() : base(true) 
        { 
        }
        public ResultadoSucesso(string mensagem) : base(true,mensagem)
        {
        }
    }
}
