namespace PersonalHub.Domain.Entidades
{
    public interface IRendimento
    {
        int Id { get; set; }
        DateTime Data { get; set; }
        decimal Valor { get; set; }
        string Descritivo { get; set; }
        string Titulo { get; set; }
        string UsuarioId { get; set; }
   }
}
