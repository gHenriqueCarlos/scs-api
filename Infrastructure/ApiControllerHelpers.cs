using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScspApi.Models;

namespace ScspApi.Infrastructure;

public abstract class ApiControllerBase : ControllerBase
{

    protected ActionResult<ApiResponse<object>> ApiMessage(string message)
    => Ok(ApiResponse<object>.Ok(message: message));

    protected ActionResult<ApiResponse<object>> ApiOk(object? data, string? message = null)
        => Ok(ApiResponse<object>.Ok(data, message));

    protected ActionResult<ApiResponse<T>> ApiOk<T>(T data, string? message = null)
        => Ok(ApiResponse<T>.Ok(data, message));

    protected ActionResult<ApiResponse<object>> ApiBadRequest(string message, IEnumerable<string>? errors = null, string? code = null)
        => BadRequest(ApiResponse<object>.Fail(message, errors, code));

    protected ActionResult<ApiResponse<object>> ApiUnauthorized(string message, string? code = null)
        => Unauthorized(ApiResponse<object>.Fail(message, code: code));

    protected ActionResult<ApiResponse<object>> ApiNotFound(string message, string? code = null)
        => NotFound(ApiResponse<object>.Fail(message, code: code));


    protected static IEnumerable<string> IdentityErrors(IdentityResult result)
        => result.Errors.Select(e => $"{e.Code}: {e.Description}");
}
