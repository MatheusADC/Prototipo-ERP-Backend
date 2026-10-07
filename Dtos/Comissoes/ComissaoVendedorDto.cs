namespace TargetDesafio.Api.Dtos;

public record ComissaoVendedorDto(
    string Vendedor,
    int QuantidadeVendas,
    decimal TotalVendido,
    decimal TotalComissao,
    IReadOnlyList<VendaComissaoDto> Vendas);