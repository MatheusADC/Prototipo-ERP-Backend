namespace TargetDesafio.Api.Dtos;

public record ResultadoJurosDto(
    decimal Valor,
    DateOnly DataVencimento,
    DateOnly DataCalculo,
    int DiasAtraso,
    decimal TaxaDiariaPercentual,
    decimal Juros,
    decimal ValorTotal);