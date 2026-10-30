using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Database;
using Shared.Core.Abstractions;
using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Departments.Queries.GetBranchById;

public class GetBranchByIdQueryHandler : IQueryHandler<
    GetBranchByIdQuery, Result<IReadOnlyList<DepartmentBranchNodeDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;

    public GetBranchByIdQueryHandler(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Result<IReadOnlyList<DepartmentBranchNodeDto>, Error>> Handle(
        GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        using var connection = await _factory.OpenConnectionAsync(cancellationToken);

        // Follow parent IDs so departments sharing a slug/path cannot enter the branch.
        const string sql = """
                           WITH RECURSIVE branch AS (
                               SELECT d.*
                               FROM departments d
                               WHERE d.id = @id AND d.deleted_at IS NULL
                               UNION
                               SELECT parent.*
                               FROM departments parent
                               JOIN branch child ON parent.id = child.parent_id
                           )
                           SELECT b.id, b.parent_id, b.name, b.slug, b.path::text AS path,
                                  b.depth, b.created_at, c.child_count,
                                  c.child_count > 0 AS has_children
                           FROM branch b
                           CROSS JOIN LATERAL (
                               SELECT count(*)::int AS child_count
                               FROM departments child
                               WHERE child.parent_id = b.id AND child.deleted_at IS NULL
                           ) c
                           WHERE b.deleted_at IS NULL
                           ORDER BY b.depth
                           """;

        var branch = (await connection.QueryAsync<DepartmentBranchNodeDto>(new CommandDefinition(
            sql, new { id = query.Id }, cancellationToken: cancellationToken))).AsList();

        if (branch.Count == 0)
        {
            return DepartmentErrors.NotFound(query.Id);
        }

        return branch;
    }
}
