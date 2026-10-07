namespace TargetDesafio.Api.Dtos;

public record LoginResponse(string Token, DateTime ExpiraEm, string Nome);