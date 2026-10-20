using System.Diagnostics;
using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Core.Database;
using Shared.Kernel.Errors;
using Shared.Kernel.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;

namespace DirectoryService.Core.Features.Positions.Queries.GetPositionList;

public class GetPositionListQueryHandler : IQueryHandler<GetPositionListQuery, Result<PagedResult<PositionListItemDto>, Error>>
{
    private readonly IReadDbContext _readContext;
    private readonly IValidator<GetPositionListQuery> _validator;

    public GetPositionListQueryHandler(
        IReadDbContext readContext,
        IValidator<GetPositionListQuery> validator)
    {
        _readContext = readContext;
        _validator = validator;
    }

    public async Task<Result<PagedResult<PositionListItemDto>, Error>> Handle(
        GetPositionListQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var query = _readContext.PositionsRead;

        if (request.Search is not null)
            query = query.Where(p => EF.Functions.ILike(p.Name.Value, $"%{request.Search}%"));

        query = (request.SortBy, request.SortDirection) switch
        {
            ("name", "asc") =>
                query.OrderBy(p => p.Name.Value)
                    .ThenBy(p => p.Id),

            ("name", "desc") =>
                query.OrderByDescending(p => p.Name.Value)
                    .ThenBy(p => p.Id),

            ("createdAt", "asc") =>
                query.OrderBy(p => p.CreatedAt)
                    .ThenBy(p => p.Id),

            ("createdAt", "desc") =>
                query.OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Id),

            _ => throw new UnreachableException(),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.Page - 1) * request.PageSize;
        if (offset >= totalCount)
        {
            return new PagedResult<PositionListItemDto>([], request.Page, request.PageSize, totalCount);
        }

        query = query
            .Skip((int)offset)
            .Take(request.PageSize);

        var positionListItems = await query.Select(p => new PositionListItemDto(
            p.Id,
            p.Name.Value,
            p.CreatedAt
        )).ToListAsync(cancellationToken);

        return new PagedResult<PositionListItemDto>(
            positionListItems,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
