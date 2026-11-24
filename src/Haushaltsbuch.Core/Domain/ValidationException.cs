namespace Haushaltsbuch.Core.Domain;

/// <summary>Wird geworfen, wenn Eingaben fachlich ungültig sind. Enthält alle Fehlermeldungen.</summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors.ToList();
    }

    public ValidationException(string error) : this([error]) { }
}
