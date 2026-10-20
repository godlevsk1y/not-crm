using System.ComponentModel;

namespace DirectoryService.Contracts.Locations;

public record CreateLocationRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming. Must be unique among stored locations, including soft-deleted locations.")]
    string Name,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error.")]
    string Country,
    [property: Description("Optional address component; null and empty strings are accepted on creation. Database limit: 60 characters (exceeding it returns 500).")]
    string? Region,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error.")]
    string City,
    [property: Description("Optional address component; null and empty strings are accepted on creation. Database limit: 60 characters (exceeding it returns 500).")]
    string? District,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error.")]
    string Street,
    [property: Description("Non-whitespace address component. Not trimmed. Database limit: 60 characters; exceeding it returns 500, not a validation error.")]
    string HouseNumber,
    [property: Description("Optional; null and empty string accepted. Database limit: 10 characters (exceeding it returns 500).")]
    string? PostalCode
);
