using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using ElAhorrador.Dtos;
using Newtonsoft.Json;

namespace AmazonSuperOfertaBot.Middlewares
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogsRepository _logsRepository;

        public ApiExceptionMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
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
                httpContext.Response.ContentType = "application/json";

                ApiExceptionDto apiExceptionDto = ex switch
                {
                    _ => new()
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        ErrorMessage = ex.Message,
                    },
                };

                await _logsRepository.CreateLog("Error", ex);

                httpContext.Response.StatusCode = apiExceptionDto.StatusCode;
                string json = JsonConvert.SerializeObject(apiExceptionDto);
                await httpContext.Response.WriteAsync(json);
            }
        }

    }
}
