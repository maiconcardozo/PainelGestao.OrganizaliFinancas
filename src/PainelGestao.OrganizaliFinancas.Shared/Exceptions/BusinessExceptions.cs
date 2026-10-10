namespace PainelGestao.OrganizaliFinancas.Shared.Exceptions
{
    public class ConflictException(string message) : Exception(message);
    public class NotFoundException(string message) : Exception(message);
    public class UnauthenticatedException(string message) : Exception(message);
    public class ServiceUnavailableException(string message) : Exception(message);
    /// <summary>Validation errors grouped by field, following RevendaPRO.</summary>
    public class InputValidationException(IReadOnlyDictionary<string, string[]> errors)
        : Exception("Os dados informados são inválidos.")
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
    }
}
