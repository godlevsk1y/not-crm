using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Positions;
using Microsoft.Extensions.Logging;
using Shared.Framework.Extensions;
using Shared.Kernel.Errors;

namespace DirectoryService.Contracts.Communication.Http;

internal sealed partial class HttpCommunicationService : IDirectoryServiceCommunication
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpCommunicationService> _logger;

    public HttpCommunicationService(HttpClient httpClient, ILogger<HttpCommunicationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<DepartmentDto, Error>> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync($"departments/{departmentId}", cancellationToken);

            return await httpResponse.UnwrapEnvelopeAsync<DepartmentDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            LogErrorGettingTheDepartmentDepartmentid(departmentId, ex);

            return GeneralErrors.Internal();
        }
    }

    public async Task<Result<PositionDto, Error>> GetPositionByIdAsync(Guid positionId, CancellationToken cancellationToken)
    {
        try
        {
            using var httpResponse = await _httpClient.GetAsync($"positions/{positionId}", cancellationToken);

            return await httpResponse.UnwrapEnvelopeAsync<PositionDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            LogErrorGettingThePosition(positionId, ex);

            return GeneralErrors.Internal();
        }
    }

    [LoggerMessage(LogLevel.Error, "Error getting the position {positionId}")]
    partial void LogErrorGettingThePosition(Guid positionId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error getting the department {departmentId}")]
    partial void LogErrorGettingTheDepartmentDepartmentid(Guid departmentId, Exception exception);
}
