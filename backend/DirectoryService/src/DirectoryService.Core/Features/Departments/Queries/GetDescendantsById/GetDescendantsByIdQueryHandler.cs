using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Database;
using Shared.Core.Abstractions;
using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Departments.Queries.GetDescendantsById;

public class GetDescendantsByIdQueryHandler : IQueryHandler<
    GetDescendantsByIdQuery, Result<IReadOnlyList<DepartmentDescendantDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;

    public GetDescendantsByIdQueryHandler(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Result<IReadOnlyList<DepartmentDescendantDto>, Error>> Handle(
        GetDescendantsByIdQuery query, CancellationToken cancellationToken)
    {
        using var connection = await _factory.OpenConnectionAsync(cancellationToken);

        // Include the source row to distinguish a missing department from an empty subtree in one query.
        const string sql = """
                           SELECT d.id, d.name, d.parent_id
                           FROM departments parent
                           JOIN departments d ON d.path <@ parent.path
                           WHERE parent.id = @id
                           ORDER BY d.name, d.id
                           """;

        var departments = (await connection.QueryAsync<DepartmentDescendantDto>(new CommandDefinition(
            sql, new { id = query.Id }, cancellationToken: cancellationToken))).AsList();

        if (departments.Count == 0)
        {
            return DepartmentErrors.NotFound(query.Id);
        }

        return departments.Where(d => d.Id != query.Id).ToArray();
    }
}
