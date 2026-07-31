
using System.Text.Json;
using DirectoryService.Shared.Errors;
using FluentValidation.Results;

namespace DirectoryService.Core.Extensions;

public static class ValidationExtensions
{
    public static Error ToError(this ValidationResult validationResult)
    {
        var validationErrors = validationResult.Errors;

        IEnumerable<IReadOnlyList<ErrorMessage>> errors =
            from validationError in validationErrors
            let errorMessage = validationError.ErrorMessage
            let error = JsonSerializer.Deserialize<Error>(errorMessage)
            select error.Messages;
        
        return Error.Validation(errors.SelectMany(e => e));
    }
}