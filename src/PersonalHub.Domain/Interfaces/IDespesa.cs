namespace PersonalHub.Domain.Entidades
{
    public interface IDespesa
    {
        int Id { get; set; }
        string? Titulo { get; set; }
        string? Descritivo { get; set; }
        int? QtdParcelas { get; set; }
        decimal? ValorParcela { get; set; }
        DateTime Vencimento { get; set; }
        List<string> UsuariosEnvolvidos { get; set; }
        List<Tag> TagsDespesa { get; set; }

        void AdicionarTags(List<Tag> tags);
    }
}
