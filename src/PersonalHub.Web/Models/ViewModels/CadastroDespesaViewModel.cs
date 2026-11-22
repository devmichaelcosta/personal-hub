using PersonalHub.Domain.Entidades;
using PersonalHub.Domain.Enums;
using PersonalHub.Web.Filters;
using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Web.Models.ViewModels
{
    public class CadastroDespesaViewModel : IDespesa, IValidatableObject
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Título")]
        public string? Titulo { get; set; }

        public string? Descritivo { get; set; }

        public int? QtdParcelas { get; set; } = 1;

        [Moeda(ErrorMessage = "O Valor da Parcela deve ser numérico.")]
        [Required(ErrorMessage = "Informe o campo Valor da Parcela")]
        public decimal? ValorParcela { get; set; }

        [Required(ErrorMessage = "Informe o campo Vencimento")]
        //[DataType(DataType.DateTime, ErrorMessage = "O campo Vencimento deve ser uma data.")]
        public DateTime Vencimento { get; set; }

        [Display(Name = "Usuários Envolvidos")]
        [Required(ErrorMessage = "Informe o campo {0}")]
        public List<string> UsuariosEnvolvidos { get; set; }

        public string[] Tags { get; set; } = new string[0];

        public List<Tag> TagsDespesa { get; set; } = new List<Tag>();

        public TipoCadastroDespesa Tipo { get; set; }

        public int? QtdMeses { get; set; }

        public string ObterCssTipoCadastro()
            => Tipo == TipoCadastroDespesa.GerarParaOsProximosMeses ? "" : "visually-hidden";
        
        public void AdicionarTags(List<Tag> tags)
        {
            foreach (var item in tags)
                if (!TagsDespesa.Any(e =>e.Nome == item.Nome))
                    TagsDespesa.Add(item);
        }

        public CadastroDespesaViewModel()
        {
            TagsDespesa = new List<Tag>();
            UsuariosEnvolvidos = new List<string>();
        }

        public static CadastroDespesaViewModel CriarComValoresIniciais()
        {
            return new CadastroDespesaViewModel
            {
                QtdParcelas = 1,
                Vencimento = DateTime.Now,
                QtdMeses = 12
            };
        }

        public CadastroDespesaViewModel(string titulo, 
                                        string descritivo, 
                                        decimal valorParcela,
                                        int qtdParcelas, 
                                        DateTime vencimento, 
                                        List<string> usuariosEnvolvidos)
        {
            TagsDespesa = new List<Tag>();

            this.QtdParcelas = qtdParcelas;
            this.ValorParcela = valorParcela;
            this.Descritivo = descritivo;
            this.Titulo = titulo;
            this.Vencimento = vencimento;
            this.UsuariosEnvolvidos = usuariosEnvolvidos;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> ret = new();

            if (UsuariosEnvolvidos.Count < 1)
                ret.Add(new ValidationResult("Selecione ao menos um usuário.", new[] { "Usuarios Envolvidos" }));

            if (Tipo != TipoCadastroDespesa.RecorrenteMensal && (!QtdParcelas.HasValue || QtdParcelas < 1))
                ret.Add(new ValidationResult("A quantidade de parcelas deve ser pelo menos 1.", new[] { nameof(QtdParcelas) }));

            if (Tipo == TipoCadastroDespesa.GerarParaOsProximosMeses && (!QtdMeses.HasValue || QtdMeses < 1))
                ret.Add(new ValidationResult("Informe uma quantidade de meses válida.", new[] { nameof(QtdMeses) }));

            if (!ValorParcela.HasValue || ValorParcela <= 0)
                ret.Add(new ValidationResult("O valor deve ser maior que zero.", new[] { nameof(ValorParcela) }));

            return ret;
        }
    }
}
