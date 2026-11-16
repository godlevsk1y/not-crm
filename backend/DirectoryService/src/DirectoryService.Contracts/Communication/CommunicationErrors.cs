using Shared.Kernel.Errors;

namespace DirectoryService.Contracts.Communication;

internal static class CommunicationErrors
{
    internal static Error InvalidResponse() =>
        Error.Internal(new ErrorMessage(
            "directory.invalid_response", 
            "Response from Directory Service is invalid"));
    
    internal static Error Timeout() =>
        Error.Internal(new ErrorMessage(
            "directory.timeout", 
            "DirectoryService did not respond within the configured timeout"));
    
    internal static Error ServiceUnavailable() =>
        Error.Internal(new ErrorMessage(
            "directory.unavailable", 
            "DirectoryService is unavailable"));
}