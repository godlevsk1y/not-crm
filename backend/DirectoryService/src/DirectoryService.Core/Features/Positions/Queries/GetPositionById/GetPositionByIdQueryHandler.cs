using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Positions;
using DirectoryService.Core.Database;
using Shared.Core.Abstractions;
using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Positions.Queries.GetPositionById;

public class GetPositionByIdQueryHandler : IQueryHandler<GetPositionByIdQuery, Result<PositionDto, Error>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetPositionByIdQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<PositionDto, Error>> Handle(GetPositionByIdQuery query, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT p.id,
                                  p.name
                           FROM positions p
                           WHERE p.id = @id AND p.deleted_at IS NULL
                           """;

        var position = await connection.QuerySingleOrDefaultAsync<PositionDto>(new CommandDefinition(
            sql, new { id = query.Id }, cancellationToken: cancellationToken));

        if (position is null)
            return PositionErrors.NotFound(query.Id);

        return position;
    }
}
