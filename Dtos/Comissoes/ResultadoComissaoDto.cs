namespace TargetDesafio.Api.Dtos;

public record ResultadoComissaoDto(
    IReadOnlyList<ComissaoVendedorDto> Vendedores,
    decimal TotalVendido,
    decimal TotalComissao);