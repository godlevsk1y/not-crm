using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using Shared.Framework.Results;
using Shared.Kernel.Errors;

namespace Shared.Framework.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<Result<TResponse, Error>> UnwrapEnvelopeAsync<TResponse>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            var jsonResponse = await response.Content.ReadFromJsonAsync<Envelope<TResponse>>(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return jsonResponse?.Error ?? GeneralErrors.Internal();
            }

            if (jsonResponse is null)
            {
                return GeneralErrors.Internal();
            }

            if (jsonResponse is { IsError: true, Error: not null })
            {
                return jsonResponse.Error;
            }

            if (jsonResponse.Result is null)
            {
                return GeneralErrors.Internal();
            }

            return jsonResponse.Result;
        }
        catch (Exception)
        {
            return GeneralErrors.Internal();
        }
    }
}