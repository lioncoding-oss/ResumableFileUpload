using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using ResumableFileUpload.Server.Configurations;
using ResumableFileUpload.Server.Database;
using ResumableFileUpload.Server.Services;
using System;
using tusdotnet.Helpers;
using tusdotnet.Models;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;
using static ResumableFileUpload.Server.Services.UploadService;

namespace ResumableFileUpload.Server.Startup;

public static class DependencyInjection
{
    public const string TusCorsPolicyName = "TusCorsPolicy";

    internal static void ConfigureKestrelFileUpload(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            var maxRequestBodySize = builder.Configuration.GetValue<int>("Tus:MaxRequestBodySize");
            options.Limits.MaxRequestBodySize = maxRequestBodySize == 0 ? null : maxRequestBodySize;
        });
    }


    internal static void AddTusServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TusDiskStorageOptions>()
            .BindConfiguration("Tus")
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .PostConfigure(ConfigureStoragePath);

        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<TusDiskStorageOptions>>().Value);

        var tuskDiskStorageOption = configuration.GetSection("Tus").Get<TusDiskStorageOptions>();

        // Clean up service configuration
        var tusDefaultConfig = new DefaultTusConfiguration
        {
            Store = new TusDiskStore(tuskDiskStorageOption?.StorageDiskPath),
            Expiration = tuskDiskStorageOption switch
            {
                { EnableExpiration: true, AbsoluteExpiration: true } => new AbsoluteExpiration(
                    TimeSpan.FromSeconds(tuskDiskStorageOption.ExpirationInSeconds)),
                { EnableExpiration: true, AbsoluteExpiration: false } => new SlidingExpiration(
                    TimeSpan.FromSeconds(tuskDiskStorageOption.ExpirationInSeconds)),
                _ => null
            }
        };

        services.AddSingleton(tusDefaultConfig);

        services.AddQuartz(q =>
        {
            if (tuskDiskStorageOption is not { EnableExpiration: true }) return;

            var jobKey = new JobKey(nameof(ExpiredFilesCleanupJob));
            q.AddJob<ExpiredFilesCleanupJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ExpiredFilesCleanupJob)}-trigger")
                .WithSimpleSchedule(s => s
                    .WithInterval(TimeSpan.FromSeconds(tuskDiskStorageOption.ExpirationInSeconds))
                    .RepeatForever())
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }


    internal static void ConfigureTusCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: TusCorsPolicyName, policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(CorsHelper.GetExposedHeaders());
            });
        });
    }


    internal static void AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options => { options.UseSqlite("Data Source=database.db"); });
        services.AddIdentityApiEndpoints<IdentityUser>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        })
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}