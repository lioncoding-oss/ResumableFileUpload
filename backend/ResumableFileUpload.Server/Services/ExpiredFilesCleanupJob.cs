using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Expiration;

namespace ResumableFileUpload.Server.Services;

[DisallowConcurrentExecution]
public sealed class ExpiredFilesCleanupJob : IJob
{
    private readonly ITusExpirationStore _expirationStore;
    private readonly ExpirationBase? _expiration;
    private readonly ILogger<ExpiredFilesCleanupJob> _logger;

    public ExpiredFilesCleanupJob(ILogger<ExpiredFilesCleanupJob> logger, DefaultTusConfiguration config)
    {
        _logger = logger;
        _expirationStore = (ITusExpirationStore)config.Store;
        _expiration = config.Expiration;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Running cleanup job...");
            var numberOfRemovedFiles = await _expirationStore.RemoveExpiredFilesAsync(context.CancellationToken);
            _logger.LogInformation(
                "Removed {NumberOfRemovedFiles} expired files. Scheduled to run again in {TimeoutTotalMilliseconds} ms",
                numberOfRemovedFiles, _expiration?.Timeout.TotalMilliseconds);
        }
        catch (Exception exc)
        {
            _logger.LogError("Failed to run cleanup job {ExceptionMessage}", exc.Message);
        }
    }
}