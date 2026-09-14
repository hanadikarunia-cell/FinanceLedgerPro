namespace FinanceLedger.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Content-Security-Policy"] = "default-src 'self'";
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        headers.Remove("X-Powered-By");
        headers.Remove("Server");

        await _next(context);
    }
}
