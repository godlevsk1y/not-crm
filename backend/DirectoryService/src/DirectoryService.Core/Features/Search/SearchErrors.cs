using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Search;

public static class SearchErrors
{
    public static Error QueryInvalid() => Error.Validation(new ErrorMessage(
        "search.query.invalid", "The trimmed search query must contain at least 2 characters.", "Q"));

    public static Error LimitInvalid() => Error.Validation(new ErrorMessage(
        "search.limit.invalid", "The limitPerType parameter must be from 1 to 100.", "LimitPerType"));

    public static Error PageInvalid() => Error.Validation(new ErrorMessage(
        "search.page.invalid", "The page parameter must be greater than 0.", "Page"));

    public static Error PageSizeInvalid() => Error.Validation(new ErrorMessage(
        "search.page.size.invalid", "The pageSize parameter must be from 1 to 100.", "PageSize"));

    public static Error EntityTypeNotFound() => Error.NotFound(new ErrorMessage(
        "search.entity.type.not.found", "The entityType must be departments, locations or positions.", "EntityType"));
}
