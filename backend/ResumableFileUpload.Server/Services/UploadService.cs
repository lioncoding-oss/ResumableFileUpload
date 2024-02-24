using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFileUpload.Server.Configurations;
using System;
using System.IO;
using System.Threading.Tasks;
using tusdotnet.Models;
using tusdotnet.Models.Concatenation;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;
using tusdotnet.Stores.FileIdProviders;

namespace ResumableFileUpload.Server.Services;

public static class UploadService
{
    internal static void ConfigureStoragePath(TusDiskStorageOptions options)
    {
        if (Directory.Exists(options.StorageDiskPath)) return;
        Directory.CreateDirectory(options.StorageDiskPath);
    }


    internal static Task<DefaultTusConfiguration> TusConfigurationFactory(HttpContext httpContext)
    {
        var tusDiskStorageOption = httpContext.RequestServices.GetRequiredService<TusDiskStorageOptions>();

        var config = new DefaultTusConfiguration
        {
            Store = new TusDiskStore(directoryPath: tusDiskStorageOption.StorageDiskPath,
                deletePartialFilesOnConcat: false,
                bufferSize: TusDiskBufferSize.Default,
                fileIdProvider: new GuidFileIdProvider()),

            MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
            MaxAllowedUploadSizeInBytes = tusDiskStorageOption.MaxRequestBodySize == 0
                ? null
                : tusDiskStorageOption.MaxRequestBodySize,
            UsePipelinesIfAvailable = true,
            Events = new Events
            {
                OnBeforeCreateAsync = HandleOnBeforeCreateAsync,
                OnFileCompleteAsync = HandleOnFileCompleteAsync,
            },
        };

        if (tusDiskStorageOption.EnableExpiration)
        {
            config.Expiration = tusDiskStorageOption.AbsoluteExpiration
                ? new AbsoluteExpiration(TimeSpan.FromSeconds(tusDiskStorageOption.ExpirationInSeconds))
                : new SlidingExpiration(TimeSpan.FromSeconds(tusDiskStorageOption.ExpirationInSeconds));
        }

        return Task.FromResult(config);
    }



    private static Task HandleOnBeforeCreateAsync(BeforeCreateContext ctx)
    {
        if (ctx.FileConcatenation is FileConcatPartial)
        {
            return Task.CompletedTask;
        }

        if (!ctx.Metadata.TryGetValue("name", out Metadata? nameValue) || nameValue.HasEmptyValue)
        {
            ctx.FailRequest("name metadata must be specified. ");
        }

        if (!ctx.Metadata.TryGetValue("type", out Metadata? contentTypeValue) || contentTypeValue.HasEmptyValue)
        {
            ctx.FailRequest("contentType metadata must be specified. ");
        }

        return Task.CompletedTask;
    }


    private static async Task HandleOnFileCompleteAsync(FileCompleteContext ctx)
    {
        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        var file = await ctx.GetFileAsync();
        logger.LogInformation("File {FileId} upload finished", file.Id);
    }
}
