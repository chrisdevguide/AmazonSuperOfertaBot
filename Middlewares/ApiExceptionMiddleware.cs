using AmazonSuperOfertaBot.Data.Repositories.Interfaces;
using ElAhorrador.Dtos;
using Newtonsoft.Json;

namespace AmazonSuperOfertaBot.Middlewares
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;
        private readonly ILogsRepository _logsRepository;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _logsRepository = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<ILogsRepository>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                httpContext.Response.ContentType = "application/json";

                ApiExceptionDto apiExceptionDto = ex switch
                {
                    _ => new()
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        ErrorMessage = ex.Message,
                    },
                };

                await _logsRepository.CreateLog(new()
                {
                    Type = "Error",
                    Data = JsonConvert.SerializeObject(ex),
                });

                httpContext.Response.StatusCode = apiExceptionDto.StatusCode;
                string json = JsonConvert.SerializeObject(apiExceptionDto);
                await httpContext.Response.WriteAsync(json);
            }
        }

    }
}
