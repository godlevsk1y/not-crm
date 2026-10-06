using Microsoft.AspNetCore.Http;
using Shared.Kernel.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Shared.Framework.Results;

public static class EndpointResults
{
    public static IResult Ok<T>(T result) =>
        TypedResults.Ok(Envelope<T>.Ok(result));
    
    public static IResult Created<T>(string location, T result)
        => TypedResults.Created(location, Envelope<T>.Ok(result));
    
    public static IResult NoContent() 
        => TypedResults.NoContent();

    public static IResult Error(Error error)
    {
        var envelope = Envelope.Failure(error);

        return error.Type switch
        {
            ErrorType.Internal or ErrorType.Failure => 
                TypedResults.InternalServerError(envelope),
            
            ErrorType.Validation or ErrorType.Domain => 
                TypedResults.BadRequest(envelope),
            
            ErrorType.NotFound =>
                TypedResults.NotFound(envelope),
            
            ErrorType.Conflict => 
                TypedResults.Conflict(envelope),
            
            _ => TypedResults.InternalServerError(envelope),
        };
    }
}