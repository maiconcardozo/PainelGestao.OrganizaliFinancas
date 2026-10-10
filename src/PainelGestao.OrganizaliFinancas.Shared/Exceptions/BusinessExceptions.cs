namespace PainelGestao.OrganizaliFinancas.Shared.Exceptions
{
    /// <summary>Validation errors grouped by field, following RevendaPRO.</summary>
    public class InputValidationException(IReadOnlyDictionary<string, string[]> errors)
        : Exception("Os dados informados são inválidos.")
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
    }
}
