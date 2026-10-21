using ErrorOr;
using FastEndpoints;
using FluentValidation.Results;

namespace OxyKit.Mail.Common;

/// <summary>
/// FastEndpoints base with shared ErrorOr → HTTP mapping for OxyKit.Mail.
/// </summary>
public abstract class MailEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected Task<bool> SendOnErrorAsync(IErrorOr result, CancellationToken ct = default) =>
        ErrorOrHttp.SendAsync(
            result,
            ValidationFailures,
            Send.NotFoundAsync,
            Send.ForbiddenAsync,
            Send.UnauthorizedAsync,
            Send.ErrorsAsync,
            ct
        );
}

/// <inheritdoc cref="MailEndpoint{TRequest, TResponse}"/>
public abstract class MailEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : notnull
{
    protected Task<bool> SendOnErrorAsync(IErrorOr result, CancellationToken ct = default) =>
        ErrorOrHttp.SendAsync(
            result,
            ValidationFailures,
            Send.NotFoundAsync,
            Send.ForbiddenAsync,
            Send.UnauthorizedAsync,
            Send.ErrorsAsync,
            ct
        );
}

/// <inheritdoc cref="MailEndpoint{TRequest, TResponse}"/>
public abstract class MailEndpointWithoutRequest : EndpointWithoutRequest
{
    protected Task<bool> SendOnErrorAsync(IErrorOr result, CancellationToken ct = default) =>
        ErrorOrHttp.SendAsync(
            result,
            ValidationFailures,
            Send.NotFoundAsync,
            Send.ForbiddenAsync,
            Send.UnauthorizedAsync,
            Send.ErrorsAsync,
            ct
        );
}

/// <inheritdoc cref="MailEndpoint{TRequest, TResponse}"/>
public abstract class MailEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse>
{
    protected Task<bool> SendOnErrorAsync(IErrorOr result, CancellationToken ct = default) =>
        ErrorOrHttp.SendAsync(
            result,
            ValidationFailures,
            Send.NotFoundAsync,
            Send.ForbiddenAsync,
            Send.UnauthorizedAsync,
            Send.ErrorsAsync,
            ct
        );
}

/// <summary>
/// Maps <see cref="ErrorOr"/> failures to FastEndpoints responses.
/// </summary>
file static class ErrorOrHttp
{
    public static async Task<bool> SendAsync(
        IErrorOr result,
        List<ValidationFailure> failures,
        Func<CancellationToken, Task> notFound,
        Func<CancellationToken, Task> forbidden,
        Func<CancellationToken, Task> unauthorized,
        Func<int, CancellationToken, Task> errors,
        CancellationToken ct
    )
    {
        if (!result.IsError)
            return false;

        var errorsList = result.Errors!;
        var first = errorsList[0];
        switch (first.Type)
        {
            case ErrorType.NotFound:
                await notFound(ct);
                return true;
            case ErrorType.Forbidden:
                await forbidden(ct);
                return true;
            case ErrorType.Unauthorized:
                await unauthorized(ct);
                return true;
            default:
                foreach (var error in errorsList)
                    failures.Add(new ValidationFailure("generalErrors", error.Description));

                var status = first.Type switch
                {
                    ErrorType.Validation => StatusCodes.Status400BadRequest,
                    ErrorType.Conflict => StatusCodes.Status409Conflict,
                    ErrorType.Failure => StatusCodes.Status500InternalServerError,
                    ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
                    _ => StatusCodes.Status400BadRequest,
                };
                await errors(status, ct);
                return true;
        }
    }
}
