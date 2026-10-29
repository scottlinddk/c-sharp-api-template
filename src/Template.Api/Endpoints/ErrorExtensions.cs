using Microsoft.AspNetCore.Http.HttpResults;
using Template.Application.Common;

namespace Template.Api.Endpoints;

internal static class ErrorExtensions
{
    /// <summary>Maps an expected <see cref="Error"/> to an RFC 9457 problem response.</summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            statusCode: status,
            title: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
