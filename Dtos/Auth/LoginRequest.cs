using System.ComponentModel.DataAnnotations;

namespace TargetDesafio.Api.Dtos;

public class LoginRequest
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Senha { get; set; } = string.Empty;
}