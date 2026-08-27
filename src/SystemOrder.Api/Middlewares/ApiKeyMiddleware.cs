namespace SystemOrder.Api.Middlewares;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(
                ApiKeyHeaderName,
                out var providedApiKey))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    message = "API Key is required."
                });

            return;
        }

        var configuredApiKey =
            _configuration["ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredApiKey) ||
            !string.Equals(
                configuredApiKey,
                providedApiKey,
                StringComparison.Ordinal))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    message = "Invalid API Key."
                });

            return;
        }

        await _next(context);
    }
}