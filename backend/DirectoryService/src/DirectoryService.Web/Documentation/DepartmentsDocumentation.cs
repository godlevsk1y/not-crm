using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Departments.Commands.TransferDepartment;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentPositions;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Positions;
using DirectoryService.Domain.Models.Errors;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Web.Controllers;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

internal static class DepartmentsDocumentation
{
    private static readonly ApiError[] NameAndSlugValidation =
    [
        new(Validation(ValueObjectErrors.DepartmentName.Empty()),
            "Name is empty or whitespace (PATCH validates only supplied non-null names)."),
        new(Validation(ValueObjectErrors.DepartmentName.TooLong()),
            "Trimmed name exceeds 100 characters (PATCH validates only supplied non-null names)."),
        new(Validation(ValueObjectErrors.Slug.Empty()),
            "Slug is empty or whitespace; PATCH validates only supplied non-null values."),
        new(Validation(ValueObjectErrors.Slug.Invalid()),
            "Slug does not match ^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$; no trimming is performed."),
    ];

    private static readonly ApiError[] PaginationValidation =
    [
        new(Error.Validation(new ErrorMessage("departments.page.invalid", "The page parameter must be greater than 0.", nameof(GetDepartmentTreeQuery.Page))),
            "Page is less than 1."),
        new(Error.Validation(new ErrorMessage("departments.page.size.invalid", "The page size must be from 5 to 100.", nameof(GetDepartmentTreeQuery.PageSize))),
            "PageSize is outside 5–100."),
    ];

    private static readonly ApiError[] MissingDepartment =
    [
        new(DepartmentErrors.NotFound(ContractExamples.Id),
            "Active department with the route ID was not found."),
    ];

    private static readonly ApiError[] LinkDepartmentMissing =
    [
        new(DepartmentErrors.NotFound(ContractExamples.Id),
            "Active department with departmentId is missing, or is removed before the relationship is saved."),
    ];

    public static ApiError[] Errors(string action) => action switch
    {
        nameof(DepartmentsController.Create) =>
        [
            ..NameAndSlugValidation,
            new(DepartmentErrors.NotFound(ContractExamples.Id),
                "Requested parentId does not identify an active department, or the parent is removed before saving."),
            new(LocationErrors.NotFound(ContractExamples.Id),
                "A locationIds entry does not identify an active location, or a location is removed before saving the relationships."),
        ],
        nameof(DepartmentsController.Update) =>
        [
            ..NameAndSlugValidation,
            new(ModelErrors.Department.ParentToItself(),
                "PATCH parentId equals the department ID."),
            new(DepartmentErrors.NotFound(ContractExamples.Id),
                "Active department or requested parentId is missing, including a parent foreign key race while saving."),
        ],
        nameof(DepartmentsController.Restore) =>
        [
            new(DepartmentErrors.NotFound(ContractExamples.Id),
                "Record is missing or has been physically deleted."),
            new(DepartmentErrors.ParentArchived(ContractExamples.Id),
                "Parent is archived; restore the parent first. The department remains archived."),
        ],
        nameof(DepartmentsController.Delete) =>
        [
            ..MissingDepartment,
            new(DepartmentErrors.HasActiveChildren(ContractExamples.Id),
                "Department has at least one active immediate child."),
        ],
        nameof(DepartmentsController.GetById) =>
        [
            ..MissingDepartment,
        ],
        nameof(DepartmentsController.TransferDepartment) =>
        [
            new(Error.Validation(new ErrorMessage("department.id.empty", "Department Id cannot be empty", nameof(TransferDepartmentCommand.DepartmentId))),
                "Department route ID is the empty UUID."),
            new(DepartmentErrors.ParentNotFound(ContractExamples.Id),
                "Requested new parent is missing (including the empty UUID)."),
            new(DepartmentErrors.ParentToSelf(),
                "newParentId equals the department ID."),
            new(DepartmentErrors.Cycle(ContractExamples.Id, ContractExamples.Id),
                "Requested parent is a descendant of the department."),
            ..MissingDepartment,
        ],
        nameof(DepartmentsController.GetDepartmentPositions) =>
        [
            ..MissingDepartment,
            new(Error.Validation(new ErrorMessage("positions.page.invalid", "The page parameter must be greater than 0.", nameof(GetDepartmentPositionsQuery.Page))),
                "Page is less than 1."),
            new(Error.Validation(new ErrorMessage("positions.page.size.invalid", "The page size must be from 5 to 100.", nameof(GetDepartmentPositionsQuery.PageSize))),
                "PageSize is outside 5–100."),
            new(Error.Validation(new ErrorMessage("positions.sort.by.invalid", "The sortBy parameter only allows 'name' or 'createdAt' values.", nameof(GetDepartmentPositionsQuery.SortBy))),
                "SortBy is not name or createdAt (case-sensitive)."),
            new(Error.Validation(new ErrorMessage("positions.sort.direction.invalid", "The sortDirection parameter only allows 'asc' or 'desc' values.", nameof(GetDepartmentPositionsQuery.SortDirection))),
                "SortDirection is not asc or desc (case-sensitive)."),
        ],
        nameof(DepartmentsController.GetDepartmentList) =>
        [
            ..PaginationValidation,
            new(Error.Validation(new ErrorMessage("departments.search.too.long", "The search string must not exceed 100 characters.", nameof(GetDepartmentListQuery.Search))),
                "Search exceeds 100 characters."),
            new(Error.Validation(new ErrorMessage("departments.sort.by.invalid", "The sortBy parameter only allows 'name' or 'createdAt' values.", nameof(GetDepartmentListQuery.SortBy))),
                "SortBy is not name or createdAt (case-sensitive)."),
            new(Error.Validation(new ErrorMessage("departments.sort.direction.invalid", "The sortBy parameter only allows 'asc' or 'desc' values.", nameof(GetDepartmentListQuery.SortDirection))),
                "SortDirection is not asc or desc (case-sensitive)."),
        ],
        nameof(DepartmentsController.GetDepartmentTree) =>
        [
            ..PaginationValidation,
        ],
        nameof(DepartmentsController.GetDepartmentTreeByName) =>
        [
            ..PaginationValidation,
            new(Error.Validation(new ErrorMessage("departments.search.query.invalid", "Search query must be at least 2 characters long.", nameof(GetDepartmentTreeByNameQuery.Search))),
                "Search (query parameter q) has fewer than 2 characters. Omitted q can instead fail model binding."),
        ],
        nameof(DepartmentsController.GetChildrenByParentId) =>
        [
            ..PaginationValidation,
        ],
        nameof(DepartmentsController.GetDescendantsById) =>
        [
            new(DepartmentErrors.NotFound(ContractExamples.Id),
                "Department with the route ID is missing or has been physically deleted. Archived departments are included."),
        ],
        nameof(DepartmentsController.GetBranchById) =>
        [
            ..MissingDepartment,
        ],
        nameof(DepartmentsController.GetAncestorsById) =>
        [
            ..PaginationValidation,
        ],
        nameof(DepartmentsController.AddLocation) =>
        [
            ..LinkDepartmentMissing,
            new(LocationErrors.NotFound(ContractExamples.Id),
                "Active location with locationId is missing, or is removed before the relationship is saved."),
            new(DepartmentErrors.LocationAlreadyAdded(ContractExamples.Id, ContractExamples.Id),
                "The relationship already exists, including a concurrent insert detected by the unique constraint."),
        ],
        nameof(DepartmentsController.AddPosition) =>
        [
            ..LinkDepartmentMissing,
            new(PositionErrors.NotFound(ContractExamples.Id),
                "Active position is missing, or a position foreign key race occurs while linking."),
            new(DepartmentErrors.PositionAlreadyAdded(ContractExamples.Id, ContractExamples.Id),
                "The relationship already exists, including a concurrent insert detected by the unique constraint."),
        ],
        nameof(DepartmentsController.RemoveLocation) =>
        [
            new(DepartmentErrors.DepartmentLocationNotFound(ContractExamples.Id, ContractExamples.Id),
                "The relationship does not exist; resource existence is not checked separately."),
        ],
        nameof(DepartmentsController.RemovePosition) =>
        [
            new(DepartmentErrors.DepartmentPositionNotFound(ContractExamples.Id, ContractExamples.Id),
                "The relationship does not exist; resource existence is not checked separately."),
        ],
        _ => [],
    };

    private static Error Validation(Error error) => Error.Validation(error.Messages);
}
