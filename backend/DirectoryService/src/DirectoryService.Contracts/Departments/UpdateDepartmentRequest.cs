using System.ComponentModel;

namespace DirectoryService.Contracts.Departments;

public record UpdateDepartmentRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming. Omitted or null: unchanged; empty or whitespace: 400.")]
    string? Name,
    [property: Description("Lowercase letters a-z, digits and hyphens; no leading/trailing hyphen. Not trimmed. No application length limit; the database limit is 100 characters (exceeding it returns 500). Omitted or null: unchanged; empty or whitespace: 400.")]
    string? Slug,
    [property: Description("Omitted or null: unchanged. Empty UUID: make root. Other UUID: active parent. Only this department path/depth is recalculated; descendants are unchanged. Use the transfer operation for a subtree.")]
    Guid? ParentId);
