using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TargetDesafio.Api.Data;
using TargetDesafio.Api.Domain;
using TargetDesafio.Api.Dtos;
using TargetDesafio.Api.Services;

namespace TargetDesafio.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<Usuario> hasher, ITokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Login == req.Login, ct);
        var ok = usuario is not null &&
                 hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, req.Senha) != PasswordVerificationResult.Failed;

        if (!ok) return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Usuário ou senha inválidos.");

        var (token, expira) = tokens.Gerar(usuario!);
        return new LoginResponse(token, expira, usuario!.Nome);
    }
}

[ApiController, Authorize, Route("api/comissoes")]
public class ComissoesController(IComissaoService service, IVendasExemploProvider exemplo) : ControllerBase
{
    [HttpGet("vendas-exemplo")]
    public async Task<VendasRequest> VendasExemplo(CancellationToken ct) => await exemplo.ObterAsync(ct);

    [HttpPost("calcular")]
    public ActionResult<ResultadoComissaoDto> Calcular(VendasRequest req) => service.Calcular(req.Vendas);
}

[ApiController, Authorize, Route("api/estoque")]
public class EstoqueController(IEstoqueService service) : ControllerBase
{
    [HttpGet("produtos")]
    public Task<List<ProdutoDto>> Produtos(CancellationToken ct) => service.ListarProdutosAsync(ct);

    [HttpGet("movimentacoes")]
    public Task<List<MovimentacaoDto>> Movimentacoes([FromQuery] int? codigoProduto, CancellationToken ct) =>
        service.ListarMovimentacoesAsync(codigoProduto, ct);

    [HttpGet("movimentacoes/{id:int}", Name = "ObterMovimentacao")]
    public Task<MovimentacaoDto> Obter(int id, CancellationToken ct) => service.ObterMovimentacaoAsync(id, ct);

    [HttpPost("movimentacoes")]
    public async Task<ActionResult<MovimentacaoDto>> Registrar(NovaMovimentacaoRequest req, CancellationToken ct)
    {
        var mov = await service.RegistrarAsync(req, ct);
        return CreatedAtRoute("ObterMovimentacao", new { id = mov.Numero }, mov);
    }
}

[ApiController, Authorize, Route("api/juros")]
public class JurosController(IJurosService service) : ControllerBase
{
    [HttpPost("calcular")]
    public ActionResult<ResultadoJurosDto> Calcular(JurosRequest req) =>
        service.Calcular(req.Valor, req.DataVencimento!.Value);
}