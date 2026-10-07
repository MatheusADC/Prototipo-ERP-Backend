using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TargetDesafio.Api.Domain;

namespace TargetDesafio.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        if (!await db.Usuarios.AnyAsync())
        {
            var admin = new Usuario { Login = "admin", Nome = "Administrador" };
            admin.SenhaHash = hasher.HashPassword(admin, "Admin@123");
            db.Usuarios.Add(admin);
        }

        if (!await db.Produtos.AnyAsync())
        {
            db.Produtos.AddRange(
                new Produto { Codigo = 101, Descricao = "Caneta Azul", Estoque = 150 },
                new Produto { Codigo = 102, Descricao = "Caderno Universitário", Estoque = 75 },
                new Produto { Codigo = 103, Descricao = "Borracha Branca", Estoque = 200 },
                new Produto { Codigo = 104, Descricao = "Lápis Preto HB", Estoque = 320 },
                new Produto { Codigo = 105, Descricao = "Marcador de Texto Amarelo", Estoque = 90 });
        }

        await db.SaveChangesAsync();
    }
}