using System.Text.Json.Serialization;

namespace DirectoryService.Shared.Errors;

public record Error
{
    public IReadOnlyList<ErrorMessage> Messages { get; } = [];
    
    public ErrorType Type { get; }

    [JsonConstructor]
    private Error(IReadOnlyList<ErrorMessage> messages, ErrorType type)
    {
        Messages = messages;
        Type = type;
    }
    
    private Error(IEnumerable<ErrorMessage> messages, ErrorType errorType)
    {
        Messages = messages.ToArray();
        Type = errorType;
    }
    
    public static Error Validation(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Validation);
    
    public static Error NotFound(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.NotFound);
    
    public static Error Conflict(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Conflict);
    
    public static Error Failure(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Failure);
    
    public static Error Internal(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Internal);
    
    public static Error Domain(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Domain);
}