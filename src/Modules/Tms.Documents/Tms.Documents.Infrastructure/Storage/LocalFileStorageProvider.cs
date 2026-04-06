using Microsoft.Extensions.Logging;
using Tms.Documents.Domain.Enums;
using Tms.Documents.Domain.Interfaces;

namespace Tms.Documents.Infrastructure.Storage;

/// <summary>
/// Local File System implementation of IStorageProvider.
/// Phase 4: เก็บไฟล์ใน wwwroot/uploads/ และ serve ผ่าน Static Files.
/// Phase 5+: แทนด้วย S3 / Azure Blob adapter โดยไม่แตะ Domain/Application.
/// </summary>
public sealed class LocalFileStorageProvider(ILogger<LocalFileStorageProvider> logger) : IStorageProvider
{
    private static readonly string UploadsDir = Path.Combine(
        AppContext.BaseDirectory, "wwwroot", "uploads");

    public Task<string> GeneratePresignedUploadUrlAsync(
        string objectKey, string contentType, CancellationToken ct = default)
    {
        // Local stub: URL จะถูก PUT โดย Client — แต่ใน Dev, Client upload ผ่าน API แทน
        // ส่ง URL กลับให้ Client เรียก POST /api/documents/upload-session/{id}/upload
        var url = $"/api/documents/local-upload/{Uri.EscapeDataString(objectKey)}";
        return Task.FromResult(url);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string objectKey, CancellationToken ct = default)
    {
        // Local: serve ผ่าน Static Files = /uploads/{objectKey}
        var url = $"/uploads/{objectKey}";
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(UploadsDir, objectKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                logger.LogInformation("Deleted local file: {ObjectKey}", objectKey);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete local file: {ObjectKey}", objectKey);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// สร้าง object key: {tenantId}/{category}/{yyyy-MM-dd}/{guid}_{filename}
    /// </summary>
    public string BuildObjectKey(Guid tenantId, DocumentCategory category, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName); // ป้องกัน path traversal
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return $"{tenantId}/{category.ToString().ToLowerInvariant()}/{DateTime.UtcNow:yyyy-MM-dd}/{uniqueId}_{safeFileName}";
    }

    /// <summary>
    /// บันทึกไฟล์จาก Stream ลง Local FS (ใช้สำหรับ Local Upload endpoint)
    /// </summary>
    public async Task<string> SaveLocalFileAsync(string objectKey, Stream content, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(UploadsDir, objectKey.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        logger.LogInformation("Saved local file: {ObjectKey}", objectKey);
        return objectKey;
    }
}
