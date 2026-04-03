using Microsoft.AspNetCore.Mvc;

public static class MvcOptionsExtensions
{
    public static IMvcBuilder AddModelValidationLogging(this IMvcBuilder builder)
    {
        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("ModelValidation");

                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    );
                var errorMessage = System.Text.Json.JsonSerializer.Serialize(errors);

                logger.LogWarning(
                    "Model validation failed for {Method} {Path} | TraceId: {TraceId} | Errors: {@ValidationErrors}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    context.HttpContext.TraceIdentifier,
                    errorMessage);

                return new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "See the errors property for details."
                });
            };
        });

        return builder;
    }
}