using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TargetDesafio.Api.Data;
using TargetDesafio.Api.Domain;
using TargetDesafio.Api.Dtos;
using TargetDesafio.Api.Infrastructure;

namespace TargetDesafio.Api.Services;

public interface ITokenService { (string Token, DateTime ExpiraEm) Gerar(Usuario usuario); }

public class TokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    public (string Token, DateTime ExpiraEm) Gerar(Usuario u)
    {
        var o = options.Value;
        var expira = time.GetUtcNow().UtcDateTime.AddMinutes(o.ExpiresMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, u.Login),
            new Claim("nome", u.Nome)
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)),
                                           SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(o.Issuer, o.Audience, claims, expires: expira, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}

public interface IComissaoService { ResultadoComissaoDto Calcular(IEnumerable<VendaDto> vendas); }

public class ComissaoService : IComissaoService
{
    public static decimal ObterPercentual(decimal valor) => valor < 100m ? 0m : valor < 500m ? 1m : 5m;

    public ResultadoComissaoDto Calcular(IEnumerable<VendaDto> vendas)
    {
        var vendedores = vendas
            .GroupBy(v => v.Vendedor.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var detalhes = g.Select(v =>
                {
                    var pct = ObterPercentual(v.Valor);
                    var comissao = Math.Round(v.Valor * pct / 100m, 2, MidpointRounding.AwayFromZero);
                    return new VendaComissaoDto(v.Valor, pct, comissao);
                }).ToList();

                return new ComissaoVendedorDto(g.Key, detalhes.Count, detalhes.Sum(d => d.Valor),
                                               detalhes.Sum(d => d.Comissao), detalhes);
            })
            .ToList();

        return new ResultadoComissaoDto(vendedores, vendedores.Sum(v => v.TotalVendido),
                                        vendedores.Sum(v => v.TotalComissao));
    }
}

public interface IVendasExemploProvider { Task<VendasRequest> ObterAsync(CancellationToken ct); }

public class VendasExemploProvider : IVendasExemploProvider
{
    public async Task<VendasRequest> ObterAsync(CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vendas.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<VendasRequest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
               ?? new VendasRequest();
    }
}

public interface IEstoqueService
{
    Task<List<ProdutoDto>> ListarProdutosAsync(CancellationToken ct);
    Task<List<MovimentacaoDto>> ListarMovimentacoesAsync(int? codigoProduto, CancellationToken ct);
    Task<MovimentacaoDto> ObterMovimentacaoAsync(int id, CancellationToken ct);
    Task<MovimentacaoDto> RegistrarAsync(NovaMovimentacaoRequest request, CancellationToken ct);
}

public class EstoqueService(AppDbContext db, TimeProvider time) : IEstoqueService
{
    public Task<List<ProdutoDto>> ListarProdutosAsync(CancellationToken ct) =>
        db.Produtos.AsNoTracking().OrderBy(p => p.Codigo)
          .Select(p => new ProdutoDto(p.Codigo, p.Descricao, p.Estoque)).ToListAsync(ct);

    public Task<List<MovimentacaoDto>> ListarMovimentacoesAsync(int? codigoProduto, CancellationToken ct) =>
        db.Movimentacoes.AsNoTracking()
          .Where(m => codigoProduto == null || m.CodigoProduto == codigoProduto)
          .OrderByDescending(m => m.Id).Take(100)
          .Select(m => new MovimentacaoDto(m.Id, m.CodigoProduto, m.Produto!.Descricao, m.Tipo, m.Descricao,
                                           m.Quantidade, m.EstoqueFinal, m.DataHoraUtc))
          .ToListAsync(ct);

    public async Task<MovimentacaoDto> ObterMovimentacaoAsync(int id, CancellationToken ct) =>
        await db.Movimentacoes.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new MovimentacaoDto(m.Id, m.CodigoProduto, m.Produto!.Descricao, m.Tipo, m.Descricao,
                                             m.Quantidade, m.EstoqueFinal, m.DataHoraUtc))
            .FirstOrDefaultAsync(ct)
        ?? throw new NaoEncontradoException($"Movimentação {id} não encontrada.");

    public async Task<MovimentacaoDto> RegistrarAsync(NovaMovimentacaoRequest req, CancellationToken ct)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Codigo == req.CodigoProduto, ct)
                      ?? throw new NaoEncontradoException($"Produto {req.CodigoProduto} não encontrado.");

        if (req.Tipo == TipoMovimentacao.Saida && produto.Estoque < req.Quantidade)
            throw new RegraNegocioException(
                $"Estoque insuficiente de {produto.Descricao}: disponível {produto.Estoque}, solicitado {req.Quantidade}.");

        produto.Estoque += req.Tipo == TipoMovimentacao.Entrada ? req.Quantidade : -req.Quantidade;

        var mov = new Movimentacao
        {
            CodigoProduto = produto.Codigo,
            Produto = produto,
            Tipo = req.Tipo,
            Descricao = req.Descricao.Trim(),
            Quantidade = req.Quantidade,
            EstoqueFinal = produto.Estoque,
            DataHoraUtc = time.GetUtcNow().UtcDateTime
        };
        db.Movimentacoes.Add(mov);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoException("O estoque deste produto foi alterado por outra operação. Tente novamente.");
        }

        return new MovimentacaoDto(mov.Id, produto.Codigo, produto.Descricao, mov.Tipo, mov.Descricao,
                                   mov.Quantidade, mov.EstoqueFinal, mov.DataHoraUtc);
    }
}

public interface IJurosService { ResultadoJurosDto Calcular(decimal valor, DateOnly vencimento); }

public class JurosService(TimeProvider time) : IJurosService
{
    public const decimal TaxaDiaria = 0.025m;

    public ResultadoJurosDto Calcular(decimal valor, DateOnly vencimento)
    {
        var hoje = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var dias = Math.Max(0, hoje.DayNumber - vencimento.DayNumber);
        var juros = Math.Round(valor * TaxaDiaria * dias, 2, MidpointRounding.AwayFromZero);
        return new ResultadoJurosDto(valor, vencimento, hoje, dias, TaxaDiaria * 100m, juros, valor + juros);
    }
}