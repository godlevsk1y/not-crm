using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Commands.RestoreLocation;

public record RestoreLocationCommand(Guid Id) : ICommand;
