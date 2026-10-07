using System.ComponentModel.DataAnnotations;
using TargetDesafio.Api.Domain;

namespace TargetDesafio.Api.Dtos;

public class NovaMovimentacaoRequest
{
    [Range(1, int.MaxValue)] public int CodigoProduto { get; set; }
    [EnumDataType(typeof(TipoMovimentacao))] public TipoMovimentacao Tipo { get; set; }
    [Required, StringLength(200)] public string Descricao { get; set; } = string.Empty;
    [Range(1, 1_000_000)] public int Quantidade { get; set; }
}