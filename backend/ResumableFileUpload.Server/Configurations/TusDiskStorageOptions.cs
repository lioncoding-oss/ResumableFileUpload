using System.ComponentModel.DataAnnotations;

namespace ResumableFileUpload.Server.Configurations;

public class TusDiskStorageOptions
{
    [Required]
    public string StorageDiskPath { get; set; }

    public int MaxRequestBodySize { get; set; }

    public bool EnableExpiration { get; set; }

    public bool AbsoluteExpiration { get; set; }

    public int ExpirationInSeconds { get; set; }
}
