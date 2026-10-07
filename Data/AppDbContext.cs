using Microsoft.EntityFrameworkCore;
using TargetDesafio.Api.Domain;

namespace TargetDesafio.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Login).HasMaxLength(60).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Login).IsUnique();
        });

        mb.Entity<Produto>(e =>
        {
            e.ToTable("Produtos", t => t.HasCheckConstraint("CK_Produtos_Estoque", "[Estoque] >= 0"));
            e.HasKey(x => x.Codigo);
            e.Property(x => x.Codigo).ValueGeneratedNever();
            e.Property(x => x.Descricao).HasMaxLength(120).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        mb.Entity<Movimentacao>(e =>
        {
            e.ToTable("Movimentacoes", t => t.HasCheckConstraint("CK_Movimentacoes_Quantidade", "[Quantidade] > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.CodigoProduto).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.CodigoProduto);
        });
    }
}