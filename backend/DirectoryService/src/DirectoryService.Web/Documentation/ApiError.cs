using System.Globalization;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

internal sealed record ApiError(Error Error, string Condition)
{
    // BadRequest here describes MVC's explicit BadRequestObjectResult; handlers
    // currently map ErrorType.BadRequest to 500 instead (see README).
    public string Status => Error.Type switch
    {
        ErrorType.Validation or ErrorType.Domain or ErrorType.BadRequest => "400",
        ErrorType.NotFound => "404",
        ErrorType.Conflict => "409",
        _ => "500",
    };

    public static string Table(IEnumerable<ApiError> entries) =>
        "| code | HTTP | error.type | Condition | invalidField | Example message |\n" +
        "| --- | --- | --- | --- | --- | --- |\n" +
        string.Join("\n", entries.SelectMany(entry => entry.Error.Messages.Select(message =>
            string.Create(CultureInfo.InvariantCulture,
                $"| `{message.Code}` | {entry.Status} | {entry.Error.Type} | {entry.Condition} | `{message.InvalidField ?? "null"}` | {message.Message} |"))));
}
