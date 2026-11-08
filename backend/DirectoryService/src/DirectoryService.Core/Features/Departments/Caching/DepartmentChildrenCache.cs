using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Kernel.Results;

namespace DirectoryService.Core.Features.Departments.Caching;

public sealed class DepartmentChildrenCache(HybridCache cache)
{
    private const string Tag = "departments:children";

    public ValueTask<PagedResult<DepartmentNodeDto>> GetOrCreateAsync(
        GetChildrenByParentIdQuery query,
        Func<CancellationToken, ValueTask<PagedResult<DepartmentNodeDto>>> factory,
        CancellationToken cancellationToken)
    {
        var key = $"{Tag}:v1:{query.ParentId:N}:{query.Page}:{query.PageSize}";

        return cache.GetOrCreateAsync(
            key, factory,
            tags: [Tag, ParentTag(query.ParentId)],
            cancellationToken: cancellationToken);
    }

    public ValueTask InvalidateAsync(IEnumerable<Guid> parentIds, CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(parentIds.Select(ParentTag), cancellationToken);

    public ValueTask InvalidateAllAsync(CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(Tag, cancellationToken);

    private static string ParentTag(Guid parentId) => $"{Tag}:parent:{parentId:N}";
}
