using TargetDesafio.Api.Domain;

namespace TargetDesafio.Api.Dtos;

public record MovimentacaoDto(
    int Numero,
    int CodigoProduto,
    string DescricaoProduto,
    TipoMovimentacao Tipo,
    string Descricao,
    int Quantidade,
    int EstoqueFinal,
    DateTime DataHoraUtc);