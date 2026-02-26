namespace Expensify.Api.Middleware;

internal static class MiddlewareExtensions
{
    internal static IApplicationBuilder UseLogContextTraceLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<LogContextTraceLoggingMiddleware>();

        return app;
    }

    internal static IApplicationBuilder UseCheckRevocatedTokens(this IApplicationBuilder app)
    {
        app.UseMiddleware<CheckRevocatedTokensMiddleware>();
        return app;
    }
}
