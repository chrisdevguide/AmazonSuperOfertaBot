using AmazonApi.Data;
using AmazonApi.Services.Implementations;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using AmazonSuperOfertaBot.Data.Repositories.Interfaces;
using AmazonSuperOfertaBot.Middlewares;
using AmazonSuperOfertaBot.Services.Implementations;
using AmazonSuperOfertaBot.Services.Interfaces;
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
            builder.Services.AddScoped<IAmazonCategoriesRepository, AmazonCategoriesRepository>();
            builder.Services.AddScoped<IAmazonProductsTelegramRepository, AmazonProductsTelegramRepository>();
            builder.Services.AddScoped<TelegramServices>();
            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                JobKey checkAmazonAlertsJobKey = new(nameof(CheckAmazonAlertsBackgroundService));
                JobKey startTelegramBotBackgroundServiceJobKey = new(nameof(StartTelegramBotBackgroundService));
                JobKey searchAmazonCategoriesBackgroundServiceJobKey = new(nameof(SearchAmazonCategoriesBackgroundService));
                JobKey vodafoneScraperJobKey = new(nameof(VodafoneScraperBackgroundService));

                q.AddJob<CheckAmazonAlertsBackgroundService>(j => j.WithIdentity(checkAmazonAlertsJobKey));
                q.AddTrigger(t => t
                    .ForJob(checkAmazonAlertsJobKey)
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever())
                    .StartNow());

                q.AddJob<StartTelegramBotBackgroundService>(j => j.WithIdentity(startTelegramBotBackgroundServiceJobKey));
                q.AddTrigger(t => t
                    .ForJob(startTelegramBotBackgroundServiceJobKey)
                    .StartNow());

                q.AddJob<SearchAmazonCategoriesBackgroundService>(j => j.WithIdentity(searchAmazonCategoriesBackgroundServiceJobKey));
                q.AddTrigger(t => t
                    .ForJob(searchAmazonCategoriesBackgroundServiceJobKey)
                    .WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever())
                    .StartNow());

                q.AddJob<VodafoneScraperBackgroundService>(j => j.WithIdentity(vodafoneScraperJobKey));
                q.AddTrigger(t => t
                    .ForJob(vodafoneScraperJobKey)
                    .WithSimpleSchedule(s => s.WithIntervalInSeconds(30).RepeatForever())
                    .StartNow());
            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            builder.Services.AddDbContext<DataContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAutoMapper(typeof(Program));

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