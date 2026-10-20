using System.ComponentModel;

namespace DirectoryService.Contracts.Locations;

public record UpdateLocationRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming. Omitted or null: unchanged; empty or whitespace: 400. Must be unique among stored locations.")]
    string? Name,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error. Omitted or null: unchanged; empty or whitespace: 400 (Domain).")]
    string? Country,
    [property: Description("Omitted, null or empty string preserves the stored value; clearing is not supported. Whitespace is stored as supplied. Database limit: 60 characters (exceeding it returns 500).")]
    string? Region,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error. Omitted or null: unchanged; empty or whitespace: 400 (Domain).")]
    string? City,
    [property: Description("Omitted, null or empty string preserves the stored value; clearing is not supported. Whitespace is stored as supplied. Database limit: 60 characters (exceeding it returns 500).")]
    string? District,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error. Omitted or null: unchanged; empty or whitespace: 400 (Domain).")]
    string? Street,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error. Omitted or null: unchanged; empty or whitespace: 400 (Domain).")]
    string? HouseNumber,
    [property: Description("Omitted, null or empty string preserves the stored value; clearing is not supported. Whitespace is stored as supplied. Database limit: 10 characters (exceeding it returns 500).")]
    string? PostalCode
);
