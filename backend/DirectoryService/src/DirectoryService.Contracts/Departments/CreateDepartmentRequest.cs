using System.ComponentModel;

namespace DirectoryService.Contracts.Departments;

public record CreateDepartmentRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming.")]
    string Name,
    [property: Description("Lowercase letters a-z, digits and hyphens; no leading/trailing hyphen. Not trimmed. No application length limit; the database limit is 100 characters (exceeding it returns 500).")]
    string Slug,
    [property: Description("Required non-null array of existing location UUIDs; [] is allowed. Send distinct IDs: repeating existing IDs can fail with 500. Unknown IDs return 404.")]
    IReadOnlyCollection<Guid> LocationIds,
    [property: Description("Omitted or null creates a root. Any UUID, including the empty UUID, must identify an active parent.")]
    Guid? ParentId = null);
