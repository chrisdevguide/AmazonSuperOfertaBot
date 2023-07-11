using AmazonApi.Data;
using ElAhorrador.Dtos;
using Newtonsoft.Json;

namespace AmazonSuperOfertaBot.Middlewares
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;
        private readonly DataContext _dataContext;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, DataContext dataContext)
        {
            _next = next;
            _logger = logger;
            _dataContext = dataContext;
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

                _dataContext.Logs.Add(new()
                {
                    Type = "Error",
                    Data = JsonConvert.SerializeObject(ex),
                });

                await _dataContext.SaveChangesAsync();

                httpContext.Response.StatusCode = apiExceptionDto.StatusCode;
                string json = JsonConvert.SerializeObject(apiExceptionDto);
                await httpContext.Response.WriteAsync(json);
            }
        }

    }
}
