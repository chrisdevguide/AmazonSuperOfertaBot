using AmazonApi.Data;
using AmazonApi.Services.Implementations;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using AmazonSuperOfertaBot.Middlewares;
using ElAhorrador.Data.Repositories.Implementations;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace AmazonApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IScrapingServices, ScrapingServices>();
            builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            builder.Services.AddScoped<ITelegramChatRepository, TelegramChatRepository>();
            builder.Services.AddScoped<IAmazonAlertRepository, AmazonAlertRepository>();
            builder.Services.AddScoped<ILogsRepository, LogsRepository>();
            builder.Services.AddScoped<TelegramServices>();
            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                JobKey jobKey = new(nameof(StartupBackgroundService));
                q.AddJob<StartupBackgroundService>(j => j.WithIdentity(jobKey));
                q.AddTrigger(t => t
                    .ForJob(jobKey)
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever())
                    .StartNow());
            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);


            builder.Services.AddDbContext<DataContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseMiddleware<ApiExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();


        }
    }
}