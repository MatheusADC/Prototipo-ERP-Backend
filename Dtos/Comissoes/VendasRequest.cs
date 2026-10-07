using System.ComponentModel.DataAnnotations;

namespace TargetDesafio.Api.Dtos;

public class VendasRequest
{
    [Required, MinLength(1)] public List<VendaDto> Vendas { get; set; } = [];
}