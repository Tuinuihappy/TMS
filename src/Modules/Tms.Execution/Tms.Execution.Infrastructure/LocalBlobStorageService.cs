using Microsoft.Extensions.Logging;
using Tms.Execution.Domain.Interfaces;

namespace Tms.Execution.Infrastructure;

/// <summary>
/// Local File System implementation of IBlobStorageService.
/// Phase 3: replace with Azure Blob / AWS S3 adapter.
/// Files stored in: ./uploads/{yyyy-MM-dd}/{fileName}
/// </summary>
public sealed class LocalBlobStorageService(ILogger<LocalBlobStorageService> logger) : IBlobStorageService
{
    private static readonly string BaseDir = Path.Combine(
        AppContext.BaseDirectory, "uploads");

    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var dir = Path.Combine(BaseDir, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(dir, uniqueName);

        await using var fs = File.Create(filePath);
        await content.CopyToAsync(fs, ct);

        var blobUrl = $"/uploads/{DateTime.UtcNow:yyyy-MM-dd}/{uniqueName}";
        logger.LogInformation("Blob uploaded: {Url}", blobUrl);
        return blobUrl;
    }

    public Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            // Convert URL back to local path
            var relativePath = blobUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob: {Url}", blobUrl);
        }
        return Task.CompletedTask;
    }
}
