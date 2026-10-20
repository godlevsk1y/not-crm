using DirectoryService.Core.Features.Positions;
using DirectoryService.Core.Features.Positions.Queries.GetPositionList;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Web.Controllers;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

internal static class PositionsDocumentation
{
    private static readonly ApiError[] NameValidation =
    [
        new(Validation(ValueObjectErrors.PositionName.Empty()),
            "Name is empty or whitespace; name is required on creation and PATCH."),
        new(Validation(ValueObjectErrors.PositionName.TooLong()),
            "Trimmed name exceeds 100 characters; name is required on creation and PATCH."),
    ];

    private static readonly ApiError[] PositionMissing =
    [
        new(PositionErrors.NotFound(ContractExamples.Id),
            "Active position with the route ID was not found."),
    ];

    public static ApiError[] Errors(string action) => action switch
    {
        nameof(PositionsController.Create) =>
        [
            ..NameValidation,
        ],
        nameof(PositionsController.Update) =>
        [
            ..NameValidation,
            ..PositionMissing,
        ],
        nameof(PositionsController.Delete) =>
        [
            ..PositionMissing,
        ],
        nameof(PositionsController.GetPositionList) =>
        [
            new(Error.Validation(new ErrorMessage("positions.page.invalid", "The page parameter must be greater than 0.", nameof(GetPositionListQuery.Page))),
                "Page is less than 1."),
            new(Error.Validation(new ErrorMessage("positions.page.size.invalid", "The page size must be from 5 to 100.", nameof(GetPositionListQuery.PageSize))),
                "PageSize is outside 5–100."),
            new(Error.Validation(new ErrorMessage("positions.search.too.long", "The search string must not exceed 100 characters.", nameof(GetPositionListQuery.Search))),
                "Search exceeds 100 characters."),
            new(Error.Validation(new ErrorMessage("positions.sort.by.invalid", "The sortBy parameter only allows 'name' or 'createdAt' values.", nameof(GetPositionListQuery.SortBy))),
                "SortBy is not name or createdAt (case-sensitive)."),
            new(Error.Validation(new ErrorMessage("positions.sort.direction.invalid", "The sortDirection parameter only allows 'asc' or 'desc' values.", nameof(GetPositionListQuery.SortDirection))),
                "SortDirection is not asc or desc (case-sensitive)."),
        ],
        _ => [],
    };

    private static Error Validation(Error error) => Error.Validation(error.Messages);
}
