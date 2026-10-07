namespace TargetDesafio.Api.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
}

public class Produto
{
    public int Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Estoque { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public enum TipoMovimentacao { Entrada = 1, Saida = 2 }

public class Movimentacao
{
    public int Id { get; set; }
    public int CodigoProduto { get; set; }
    public Produto? Produto { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int EstoqueFinal { get; set; }
    public DateTime DataHoraUtc { get; set; }
}