using System.Text.Json.Serialization;

namespace DirectoryService.Shared.Errors;

/// <summary>
/// Represents the type of error.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
    /// <summary>
    /// Indicates a general failure.
    /// </summary>
    Failure,
    /// <summary>
    /// Indicates a validation error.
    /// </summary>
    Validation, 
    /// <summary>
    /// Indicates that a resource was not found.
    /// </summary>
    NotFound,
    /// <summary>
    /// Indicates a conflict, such as a duplicate resource.
    /// </summary>
    Conflict,
    /// <summary>
    /// Indicates an internal server error.
    /// </summary>
    Internal,
    /// <summary>
    /// Indicates a domain error.
    /// </summary>
    Domain,
}
