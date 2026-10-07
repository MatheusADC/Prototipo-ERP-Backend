using System.ComponentModel.DataAnnotations;

namespace TargetDesafio.Api.Dtos;

public class JurosRequest
{
    [Range(0.01, 1_000_000_000)] public decimal Valor { get; set; }
    [Required] public DateOnly? DataVencimento { get; set; }
}