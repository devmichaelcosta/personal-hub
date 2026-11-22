using PersonalHub.Domain.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PersonalHub.Infrastructure.Data
{
    public class PersonalHubContext : IdentityDbContext<Usuario, TipoUsuario, string>
    {
        private readonly IConfiguration configuration;

        public PersonalHubContext(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            string connStr = this.configuration.GetConnectionString("ContextConnection")
                ?? throw new InvalidOperationException("Connection string 'ContextConnection' nao configurada.");
            dbContextOptionsBuilder.UseSqlServer(connStr, b => b.MigrationsAssembly("PersonalHub.Infrastructure"));

            base.OnConfiguring(dbContextOptionsBuilder);
        }

        public DbSet<Despesa> Despesas => Set<Despesa>();
        public DbSet<Rendimento> Rendimentos => Set<Rendimento>();
        public DbSet<Parcela> Parcelas => Set<Parcela>();
        public DbSet<OcorrenciaDespesaRecorrente> OcorrenciasDespesasRecorrentes => Set<OcorrenciaDespesaRecorrente>();
        public DbSet<TagDespesa> TagsDepesas => Set<TagDespesa>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Tarefa> Tarefas => Set<Tarefa>();
        public DbSet<ChatConversa> ChatConversas => Set<ChatConversa>();
        public DbSet<ChatMensagem> ChatMensagens => Set<ChatMensagem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tarefa>()
                .ToTable("Tarefas");

            modelBuilder.Entity<Tarefa>().Property(e => e.Nome).IsRequired(true);

            modelBuilder.Entity<Despesa>()
                .ToTable("Despesas");

            modelBuilder.Entity<Despesa>()
                .Property(e => e.Recorrente)
                .HasDefaultValue(false);

            modelBuilder.Entity<Despesa>()
                .Property(e => e.Descritivo)
                .HasMaxLength(500)
                .IsRequired(false);

            modelBuilder.Entity<Despesa>()
                .Property(e => e.Valor)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Despesa>()
                .Property(e => e.Titulo)
                .HasMaxLength(100)
                .IsRequired(true);

            modelBuilder.Entity<Despesa>()
                .HasMany(e => e.Parcelas)
                .WithOne(e => e.Despesa);

            modelBuilder.Entity<Tag>().ToTable("Tags");

            modelBuilder.Entity<TagDespesa>()
                .ToTable("TagsDespesas")
                .HasKey(e => new { e.DespesaId, e.TagId });

            modelBuilder.Entity<TagDespesa>()
                .HasOne(e => e.Tag)
                .WithMany(e => e.TagsDepesas)
                .HasForeignKey(e => e.TagId);

            modelBuilder.Entity<TagDespesa>()
                .HasOne(e => e.Despesa)
                .WithMany(e => e.TagsDespesas)
                .HasForeignKey(e => e.DespesaId);

            modelBuilder.Entity<Parcela>().ToTable("Parcelas");
            modelBuilder.Entity<Parcela>().HasOne(e => e.Despesa).WithMany(e => e.Parcelas).HasForeignKey(e => e.DespesaId);
            modelBuilder.Entity<Parcela>().Property(e => e.Valor).IsRequired(true);
            modelBuilder.Entity<Parcela>().Property(e => e.Vencimento).IsRequired(true);
            modelBuilder.Entity<Parcela>().Property(e => e.Valor).HasPrecision(18, 2);

            modelBuilder.Entity<OcorrenciaDespesaRecorrente>()
                .ToTable("OcorrenciasDespesasRecorrentes")
                .HasOne(e => e.Despesa)
                .WithMany()
                .HasForeignKey(e => e.DespesaId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OcorrenciaDespesaRecorrente>()
                .HasIndex(e => new { e.DespesaId, e.MesReferencia })
                .IsUnique();
            modelBuilder.Entity<OcorrenciaDespesaRecorrente>()
                .Property(e => e.MesReferencia)
                .HasColumnType("date");
            modelBuilder.Entity<OcorrenciaDespesaRecorrente>()
                .Property(e => e.Valor)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rendimento>().ToTable("Rendimentos").HasOne(e => e.Usuario).WithMany(e=>e.Rendimentos);
            modelBuilder.Entity<Rendimento>().Property(e => e.Valor).HasPrecision(18, 2);
            modelBuilder.Entity<Rendimento>().Property(e => e.Titulo).IsRequired(true);
            modelBuilder.Entity<Rendimento>().Property(e => e.Descritivo).IsRequired(false).HasMaxLength(255);

            modelBuilder.Entity<TipoUsuario>().ToTable("TiposUsuarios");

            modelBuilder.Entity<Usuario>().ToTable("Usuarios")
                .HasMany(e => e.Rendimentos).WithOne(e => e.Usuario).HasForeignKey(e=>e.UsuarioId);

            modelBuilder.Entity<Usuario>()
                .Property(e => e.SobreNome)
                .HasMaxLength(200)
                .IsRequired(true);

            modelBuilder.Entity<Usuario>()
                .Property(e => e.Nome)
                .HasMaxLength(100)
                .IsRequired(true);

            modelBuilder.Entity<UsuarioDespesa>().ToTable("UsuariosDespesas")
                .HasOne(e => e.Despesa).WithMany(e => e.UsuariosEnvolvidos).HasForeignKey(e => e.DespesaId);

            modelBuilder.Entity<ChatConversa>()
                .ToTable("ChatConversas")
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ChatConversa>()
                .HasIndex(e => e.UsuarioId)
                .IsUnique();
            modelBuilder.Entity<ChatMensagem>()
                .ToTable("ChatMensagens")
                .HasOne(e => e.Conversa)
                .WithMany(e => e.Mensagens)
                .HasForeignKey(e => e.ConversaId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ChatMensagem>().Property(e => e.Papel).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<ChatMensagem>().Property(e => e.Tipo).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<ChatMensagem>().Property(e => e.Conteudo).IsRequired();
            modelBuilder.Entity<ChatMensagem>().Property(e => e.DadosJson);
            modelBuilder.Entity<ChatMensagem>().HasIndex(e => new { e.ConversaId, e.CriadaEm });
        }
    }
}
