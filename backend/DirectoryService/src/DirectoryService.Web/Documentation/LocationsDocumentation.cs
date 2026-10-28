using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Locations.Queries.GetLocationsList;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Web.Controllers;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

internal static class LocationsDocumentation
{
    private static readonly ApiError[] WriteErrors =
    [
        new(Validation(ValueObjectErrors.LocationName.Empty()),
            "Name is empty or whitespace (PATCH validates only supplied non-null names)."),
        new(Validation(ValueObjectErrors.LocationName.TooLong()),
            "Trimmed name exceeds 100 characters (PATCH validates only supplied non-null names)."),
        new(LocationErrors.AlreadyExists("Moscow Office"),
            "Name duplicates an existing stored location, including a unique-constraint race; PATCH detects this on save."),
    ];

    private static readonly ApiError[] MissingResource =
    [
        new(LocationErrors.NotFound(ContractExamples.Id),
            "Active location with the route ID was not found."),
    ];

    public static ApiError[] Errors(string action) => action switch
    {
        nameof(LocationsController.Create) =>
        [
            ..WriteErrors,
            new(Error.Validation(ValueObjectErrors.Address.CountryEmptyMessage),
                "Country is empty or whitespace on creation."),
            new(Error.Validation(ValueObjectErrors.Address.CityEmptyMessage),
                "City is empty or whitespace on creation."),
            new(Error.Validation(ValueObjectErrors.Address.StreetEmptyMessage),
                "Street is empty or whitespace on creation."),
            new(Error.Validation(ValueObjectErrors.Address.HouseNumberEmptyMessage),
                "HouseNumber is empty or whitespace on creation."),
        ],
        nameof(LocationsController.Update) =>
        [
            ..WriteErrors,
            new(Error.Domain(ValueObjectErrors.Address.CountryEmptyMessage),
                "Supplied Country is empty or whitespace on update; null preserves its stored value."),
            new(Error.Domain(ValueObjectErrors.Address.CityEmptyMessage),
                "Supplied City is empty or whitespace on update; null preserves its stored value."),
            new(Error.Domain(ValueObjectErrors.Address.StreetEmptyMessage),
                "Supplied Street is empty or whitespace on update; null preserves its stored value."),
            new(Error.Domain(ValueObjectErrors.Address.HouseNumberEmptyMessage),
                "Supplied HouseNumber is empty or whitespace on update; null preserves its stored value."),
            ..MissingResource,
        ],
        nameof(LocationsController.Delete) =>
        [
            ..MissingResource,
        ],
        nameof(LocationsController.GetById) =>
        [
            ..MissingResource,
        ],
        nameof(LocationsController.GetLocationList) =>
        [
            new(Error.Validation(new ErrorMessage("location.search.too.long", "The search string must not exceed 100 characters.", nameof(GetLocationListQuery.Search))),
                "Search exceeds 100 characters."),
            new(Error.Validation(new ErrorMessage("locations.department.ids.invalid", "The departmentIds parameter must contain comma-separated UUIDs.", nameof(GetLocationListQuery.DepartmentIds))),
                "DepartmentIds contains an invalid UUID or an empty comma-separated entry."),
            new(Error.Validation(new ErrorMessage("location.department.count.invalid", "The minimum number of departments must be greater than or equal to zero.", nameof(GetLocationListQuery.MinDepartmentCount))),
                "Supplied MinDepartmentCount is negative."),
            new(Error.Validation(new ErrorMessage("locations.sort.by.invalid", "The sortBy parameter only allows 'name', 'createdAt' or 'departmentCount' values.", nameof(GetLocationListQuery.SortBy))),
                "SortBy is not name, createdAt or departmentCount (case-sensitive)."),
            new(Error.Validation(new ErrorMessage("locations.sort.direction.invalid", "The sortBy parameter only allows 'asc' or 'desc' values.", nameof(GetLocationListQuery.SortDirection))),
                "SortDirection is not asc or desc (case-sensitive)."),
            new(Error.Validation(new ErrorMessage("locations.page.invalid", "The page parameter must be greater than 0.", nameof(GetLocationListQuery.Page))),
                "Page is less than 1."),
            new(Error.Validation(new ErrorMessage("locations.page.size.invalid", "The page size must be from 5 to 100.", nameof(GetLocationListQuery.PageSize))),
                "PageSize is outside 5–100."),
        ],
        _ => [],
    };

    private static Error Validation(Error error) => Error.Validation(error.Messages);
}
