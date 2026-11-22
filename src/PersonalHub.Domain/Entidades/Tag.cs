namespace PersonalHub.Domain.Entidades
{
    public class Tag:Entity
    {
        public string Nome { get; set; } = string.Empty;
        public List<TagDespesa> TagsDepesas { get; set; } = new List<TagDespesa>();
        public Tag(string nome)
        {
             this.Nome = nome;  
        }
        public Tag()
        {

        }
    }
}
