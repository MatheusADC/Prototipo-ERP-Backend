namespace TargetDesafio.Api.Infrastructure;

public abstract class AppException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

public class NaoEncontradoException(string message)
    : AppException(StatusCodes.Status404NotFound, "Recurso não encontrado", message);

public class RegraNegocioException(string message)
    : AppException(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", message);

public class ConflitoException(string message)
    : AppException(StatusCodes.Status409Conflict, "Conflito de concorrência", message);