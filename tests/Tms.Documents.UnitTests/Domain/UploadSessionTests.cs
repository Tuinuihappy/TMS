using FluentAssertions;
using Xunit;
using Tms.Documents.Domain.Entities;
using Tms.Documents.Domain.Enums;

namespace Tms.Documents.UnitTests.Domain;

public class UploadSessionTests
{
    [Fact]
    public void Create_WithValidData_ReturnsActiveSession()
    {
        // Arrange
        string fileName = "doc.jpg";
        string contentType = "image/jpeg";
        long size = 1024;
        DocumentCategory category = DocumentCategory.ProofOfDelivery;
        Guid ownerId = Guid.NewGuid();
        string ownerType = "Shipment";
        string presignedUrl = "https://s3.example.com/upload";

        // Act
        var session = UploadSession.Create(fileName, contentType, size, category, ownerId, ownerType, presignedUrl, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        session.FileName.Should().Be(fileName);
        session.Status.Should().Be(UploadSessionStatus.Active);
        session.IsExpired.Should().BeFalse();
    }
}
