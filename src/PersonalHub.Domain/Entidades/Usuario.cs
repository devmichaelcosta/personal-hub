using Microsoft.AspNetCore.Identity;

namespace PersonalHub.Domain.Entidades
{
    public class Usuario : IdentityUser<string>
    {
        public string? Nome { get; set; }
        public string? SobreNome { get; set; }
        public List<Rendimento> Rendimentos { get; set; } = new List<Rendimento>();
        public List<UsuarioDespesa> UsuariosDespesas { get; set; } = new List<UsuarioDespesa>();
        protected Usuario()
        {
            Rendimentos = new List<Rendimento>();
        }

        public Usuario(string userName, string nome, string sobrenome, string email)
        {
            Rendimentos = new List<Rendimento>();
            Id = Guid.NewGuid().ToString();
            UserName = userName;
            Nome = nome;
            SobreNome= sobrenome;
            Email = email;
        } 
    }
}