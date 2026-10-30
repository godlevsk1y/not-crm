using System.Text.Json;
using System.Text.Json.Nodes;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Contracts.Positions;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Contracts.Search;
using DirectoryService.Contracts.WebApi.Departments;
using Shared.Kernel.Results;

namespace DirectoryService.Web.Documentation;

// Typed examples are serialized with the same JSON defaults as HTTP results.
internal static class ContractExamples
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly DateTime Timestamp = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyDictionary<Type, object> Requests { get; } = new Dictionary<Type, object>
    {
        [typeof(CreateDepartmentRequest)] = new CreateDepartmentRequest("Engineering", "engineering", [], null),
        [typeof(UpdateDepartmentRequest)] = new UpdateDepartmentRequest("Engineering team", null, null),
        [typeof(TransferDepartmentRequest)] = new TransferDepartmentRequest(null),
        [typeof(CreateLocationRequest)] = new CreateLocationRequest("Moscow Office", "Russia", null, "Moscow", null, "Tverskaya", "1", null),
        [typeof(UpdateLocationRequest)] = new UpdateLocationRequest(null, null, null, "Saint Petersburg", null, null, null, null),
        [typeof(CreatePositionRequest)] = new CreatePositionRequest("Engineer"),
        [typeof(UpdatePositionRequest)] = new UpdatePositionRequest("Senior engineer"),
    };

    private static readonly AddressDto Address = new("Russia", null, "Moscow", null, "Tverskaya", "1", null);

    public static IReadOnlyDictionary<Type, object> Results { get; } = new Dictionary<Type, object>
    {
        [typeof(Guid)] = Id,
        [typeof(IReadOnlyList<DepartmentBranchNodeDto>)] = new DepartmentBranchNodeDto[]
        {
            new(Id, null, "Company", "company", "company", 0, Timestamp, 2, true),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), Id,
                "Engineering", "engineering", "company.engineering", 1, Timestamp, 0, false),
        },
        [typeof(DepartmentDto)] = new DepartmentDto(Id, "Engineering", "engineering", "engineering", null),
        [typeof(TransferredDepartmentDto)] = new TransferredDepartmentDto(Id, "Engineering", "engineering", "engineering", 0, null),
        [typeof(LocationDto)] = new LocationDto(Id, "Moscow Office", Address),
        [typeof(PositionDto)] = new PositionDto(Id, "Engineer"),
        [typeof(SearchPageDto)] = new SearchPageDto([new(Id, "Engineer")], false),
        [typeof(SearchResultDto)] = new SearchResultDto(
            new([new(Id, "Engineering")], false),
            new([new(Id, "Engineering office")], false),
            new([new(Id, "Engineer")], false)),
        [typeof(PagedResult<PositionListItemDto>)] = new PagedResult<PositionListItemDto>([new(Id, "Engineer", Timestamp)], 1, 20, 1),
        [typeof(PagedResult<LocationListItemDto>)] = new PagedResult<LocationListItemDto>([new(Id, "Moscow Office", Address, Timestamp, 0)], 1, 20, 1),
        [typeof(IReadOnlyList<LocationWithDepartmentCountDto>)] = new LocationWithDepartmentCountDto[] { new(new(Id, "Moscow Office", Address), 0) },
        [typeof(PagedResult<DepartmentListItemDto>)] = new PagedResult<DepartmentListItemDto>([new(Id, "Engineering", "engineering", "engineering", true, Timestamp, Timestamp, null)], 1, 20, 1),
        [typeof(PagedResult<DepartmentNodeDto>)] = new PagedResult<DepartmentNodeDto>([new() { Id = Id, Name = "Engineering", Slug = "engineering", Path = "engineering", Depth = 0, CreatedAt = default, HasChildren = false, ChildCount = 0 }], 1, 20, 1),
        [typeof(PagedResult<DepartmentAncestorDto>)] = new PagedResult<DepartmentAncestorDto>([new(Id, "Company", "company", "company", 0, null)], 1, 20, 1),
        [typeof(IReadOnlyList<DepartmentDescendantDto>)] = new DepartmentDescendantDto[] { new(Id, "Engineering", Guid.Parse("22222222-2222-2222-2222-222222222222")) },
        [typeof(PagedResult<DepartmentWithAncestorsDto>)] = new PagedResult<DepartmentWithAncestorsDto>([new() { Id = Id, Name = "Engineering", Slug = "engineering", Path = "engineering", Depth = 0, ParentId = null, Ancestors = [] }], 1, 20, 1),
    };

    public static JsonNode? Json(object value) => JsonSerializer.SerializeToNode(value, value.GetType(), JsonOptions);
}
