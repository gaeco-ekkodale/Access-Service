// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Net;

namespace AccessService.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning a standardized error response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="logger">The logger for logging exceptions.</param>
    /// <param name="env">The hosting environment to check for development mode.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(httpContext, ex.Message, ex.GetType().Name);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, string message, string exceptionType)
    {
        context.Response.ContentType = "application/json";
        var result = GetErrorResponseForException(exceptionType, message);
        context.Response.StatusCode = result.StatusCode;
        await context.Response.WriteAsJsonAsync(result);
    }

    private ErrorResponse GetErrorResponseForException(string exceptionType, string message)
    {
        var errorResponse = new ErrorResponse { Message = message };

        switch (exceptionType)
        {
            case nameof(UnauthorizedAccessException):
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case nameof(ArgumentException):
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case nameof(NotImplementedException):
                errorResponse.StatusCode = (int)HttpStatusCode.NotImplemented;
                break;

            case nameof(InvalidOperationException):
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                errorResponse.Message = "An unexpected internal server error occurred. Please contact the admin or try again later.";
                if (_env.IsDevelopment())
                {
                    errorResponse.Message += " For development purposes: " + message;
                }
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        return errorResponse;
    }

    /// <summary>
    /// Represents a standardized error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode
        {
            get; set;
        }
    }
}
