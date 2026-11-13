namespace DirectoryService.Contracts.Communication.Http;

public record DirectoryServiceOptions(Uri BaseAddress, TimeSpan Timeout);