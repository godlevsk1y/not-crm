using System.Diagnostics.CodeAnalysis;

namespace DirectoryService.Contracts.Communication.Http;

public record DirectoryServiceOptions
{
    public required Uri BaseAddress { get; init; }
    public required TimeSpan Timeout { get; init; }

    public DirectoryServiceOptions() { }

    [SetsRequiredMembers]
    public DirectoryServiceOptions(Uri baseAddress, TimeSpan timeout)
    {
        BaseAddress = baseAddress;
        Timeout = timeout;
    }
}