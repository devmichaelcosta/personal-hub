namespace PersonalHub.Web.Models.ViewModels
{
    public class DespesaDashboard
    {
        public List<string> AvatarUsuariosEnvolvidos { get; private set; }
        public string Titulo { get; private set; }
        public string Vencimento { get; private set; }
        public decimal Valor { get; private set; }
        public decimal? ValorTotal { get; private set; }
        public bool TaVencendo { get; private set; }
        public bool Venceu { get; private set; }
        public bool AVencer { get; private set; }
        public string TituloVencimento { get; private set; } = string.Empty;
        public string ClasseCssVencimento { get; private set; } = string.Empty;
        public int DespesaId { get; private set; }
        public int ParcelaId { get; private set; }
        public int OcorrenciaId { get; private set; }
        public bool Recorrente { get; private set; }
        public bool Pago { get; private set; }
        public List<string> Tags { get;private set; }
        public string ValorFormatado { get; private set; } = string.Empty;
        private string ObterValorFormatado()
        {
            if (this.AvatarUsuariosEnvolvidos.Count > 1)
                return $"{this.Valor.ToString("N2")} de ({this.ValorTotal?.ToString("N2")})";

            return this.Valor.ToString("N2");
        }

        public DespesaDashboard(int id, string titulo, decimal valor, 
                                DateTime vencimento,int parcelaId,bool pago, List<string> avatarUsuarios,
                                List<string> tags, bool recorrente = false)
        {
            DespesaId = id;
            Pago = pago;    
            Tags = tags;
            Recorrente = recorrente;
            ParcelaId = recorrente ? 0 : parcelaId;
            OcorrenciaId = recorrente ? parcelaId : 0;
            AvatarUsuariosEnvolvidos = avatarUsuarios;
            Titulo = titulo;
            Vencimento = vencimento.ToString("dd/MM/yyyy");
             
            Venceu = DateTime.Now.Date >= vencimento.Date;
            AVencer = !Venceu && vencimento.Date <= DateTime.Now.Date.AddDays(3);
            TaVencendo = Venceu || AVencer;

            if (TaVencendo)
            {
                TituloVencimento = "Essa despesa está proxima de vencer.";
                ClasseCssVencimento = "ta-vencendo";

                if (Venceu)
                {
                    TituloVencimento = "Essa despesa venceu.";
                    ClasseCssVencimento = "venceu";
                }
            }

            Valor = valor / avatarUsuarios.Count;
            ValorTotal = avatarUsuarios.Count > 1 ? valor : ValorTotal;

            ValorFormatado = ObterValorFormatado();
        }
    }
}
