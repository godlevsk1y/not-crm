namespace Shared.Kernel.Errors;

/// <summary>
/// Represents a standardized error message.
/// </summary>
/// <param name="Code">The application-specific error code.</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="InvalidField">
/// The name of the field that caused the error, if applicable; otherwise, <see langword="null"/>.
/// </param>
public record ErrorMessage(string Code, string Message, string? InvalidField = null);