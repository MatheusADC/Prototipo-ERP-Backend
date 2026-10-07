using System.ComponentModel.DataAnnotations;

namespace TargetDesafio.Api.Dtos;

public class VendaDto
{
    [Required, StringLength(100)] public string Vendedor { get; set; } = string.Empty;
    [Range(0.01, 1_000_000_000)] public decimal Valor { get; set; }
}