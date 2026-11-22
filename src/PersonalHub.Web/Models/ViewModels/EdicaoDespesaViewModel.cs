using PersonalHub.Domain.Entidades;
using PersonalHub.Web.Filters;
using System.ComponentModel.DataAnnotations;


namespace PersonalHub.Web.Models.ViewModels
{
    public class EdicaoDespesaViewModel : IDespesa, IValidatableObject
    {
        public int Id { get; set; }

        public int ParcelaId { get; set; }

        public int OcorrenciaId { get; set; }

        public bool Recorrente { get; set; }

        [Required(ErrorMessage = "Informe o campo {0}")]
        [Display(Name = "Título")]
        public string? Titulo { get; set; }

        public string? Descritivo { get; set; }

        public int? QtdParcelas { get; set; }

        [Moeda(ErrorMessage = "O Valor da Parcela deve ser numérico.")]
        [Required(ErrorMessage = "Informe o campo Valor da Parcela")]
        public decimal? ValorParcela { get; set; }

        [Required(ErrorMessage = "Informe o campo Vencimento")]
        [DataType(DataType.DateTime, ErrorMessage = "O campo Vencimento deve ser uma data.")]
        public DateTime Vencimento { get; set; }

        [Display(Name = "Usuários Envolvidos")]
        [Required(ErrorMessage = "Informe o campo {0}")]
        public List<string> UsuariosEnvolvidos { get; set; }

        public string[] Tags { get; set; } = new string[0];

        public List<Tag> TagsDespesa { get; set; } = new List<Tag>();

        public void AdicionarTags(List<Tag> tags)
        {
            foreach (var item in tags)
                if (!TagsDespesa.Any(e => e.Nome == item.Nome))
                    TagsDespesa.Add(item);
        }

        public EdicaoDespesaViewModel()
        {
            TagsDespesa = new List<Tag>();
            UsuariosEnvolvidos = new List<string>();
        }

        private EdicaoDespesaViewModel(int id, string titulo, string descritivo, decimal valorParcela, 
                                       int qtdParcelas, DateTime vencimento, bool recorrente,
                                       List<string> usuariosEnvolvidos,List<TagDespesa> tagsDespesas)
        { 
            this.Id = id;
            this.QtdParcelas = qtdParcelas;
            this.Recorrente = recorrente;
            this.ValorParcela = valorParcela;
            this.Descritivo = descritivo;
            this.Titulo = titulo;
            this.Vencimento = vencimento;
            this.UsuariosEnvolvidos = usuariosEnvolvidos;
            
            this.Tags = tagsDespesas != null
                ? tagsDespesas.Select(e => e.Tag.Nome).ToArray()
                : new string[] {};

            this.TagsDespesa = tagsDespesas != null
                ? tagsDespesas.Select(e => e.Tag).ToList()
                : new List<Tag>();
        }

        public static EdicaoDespesaViewModel Criar(Despesa? despesa)
        {
            if (despesa == null) throw new ArgumentNullException(nameof(despesa));

            return new EdicaoDespesaViewModel(
                despesa.Id,
                despesa.Titulo ?? string.Empty,
                despesa.Descritivo,
                despesa.Recorrente
                    ? despesa.Valor.GetValueOrDefault()
                    : despesa.Valor.GetValueOrDefault() / despesa.QtdParcelas.GetValueOrDefault(1),
                despesa.QtdParcelas ?? 0, 
                despesa.Vencimento,
                despesa.Recorrente,
                despesa.UsuariosEnvolvidos.Select(e => e.UsuarioId).ToList() ?? new List<string>(),
                despesa.TagsDespesas
            );
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> ret = new();

            if (UsuariosEnvolvidos.Count < 1)
            {
                ret.Add(new ValidationResult("Selecione ao menos um usuário.", new[] { "Usuarios Envolvidos" }));
            }

            if (!Recorrente && (!QtdParcelas.HasValue || QtdParcelas < 1))
                ret.Add(new ValidationResult("A quantidade de parcelas deve ser pelo menos 1.", new[] { nameof(QtdParcelas) }));

            if (!ValorParcela.HasValue || ValorParcela <= 0)
                ret.Add(new ValidationResult("O valor deve ser maior que zero.", new[] { nameof(ValorParcela) }));

            return ret; 
        }
    }
}
