using Microsoft.AspNetCore.Identity;

namespace PersonalHub.Domain.Entidades
{
    public class TipoUsuario : IdentityRole<string>
    {
        public TipoUsuario()
        {

        }
        public TipoUsuario(string nome)
        {
            Name = nome;
            NormalizedName = nome;
            Id = Guid.NewGuid().ToString();
        }
    }
}