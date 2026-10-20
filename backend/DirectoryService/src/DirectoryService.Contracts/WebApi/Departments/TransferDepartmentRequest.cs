using System.ComponentModel;

namespace DirectoryService.Contracts.WebApi.Departments;

public record TransferDepartmentRequest(
    [property: Description("Omitted or null makes a root and transfers the whole subtree. Empty UUID is looked up as a parent (404). Self-parenting or a descendant parent returns 409.")]
    Guid? NewParentId);
