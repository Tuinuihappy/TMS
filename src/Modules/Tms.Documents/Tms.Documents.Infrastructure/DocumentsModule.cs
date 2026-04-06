using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Documents.Application.Features.CompleteUpload;
using Tms.Documents.Domain.Interfaces;
using Tms.Documents.Infrastructure.Persistence;
using Tms.Documents.Infrastructure.Persistence.Repositories;
using Tms.Documents.Infrastructure.Storage;

namespace Tms.Documents.Infrastructure;

public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<DocumentsDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "doc"))
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Storage Provider (Local File System for Phase 4)
        services.AddScoped<IStorageProvider, LocalFileStorageProvider>();
        services.AddSingleton<LocalFileStorageProvider>(); // ใช้เรียก SaveLocalFileAsync จาก Endpoint

        // MediatR — Application handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CompleteUploadHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(DocumentsModule).Assembly);
        });

        return services;
    }
}
