using CSharpFunctionalExtensions;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Database;

public interface ITransaction : IDisposable
{
    UnitResult<Error> Commit();
    
    UnitResult<Error> Rollback();
}