using System.ComponentModel;

namespace DirectoryService.Contracts.Search;

public record SearchPageDto(
    [property: Description("Active matches ordered by name ASC, id ASC; each item contains only id and name.")]
    IReadOnlyList<SearchItemDto> Items,
    [property: Description("True if another matching row exists after this page. No total count is calculated.")]
    bool HasMore);
