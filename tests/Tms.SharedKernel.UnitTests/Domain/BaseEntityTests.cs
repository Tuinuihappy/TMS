using FluentAssertions;
using Xunit;
using Tms.SharedKernel.Domain;

namespace Tms.SharedKernel.UnitTests.Domain;

public class TestEntity : BaseEntity 
{
    public void AddTestEvent(IDomainEvent e) => AddDomainEvent(e);
}

public class TestDomainEvent : IDomainEvent 
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredAt => DateTime.UtcNow;
}

public class BaseEntityTests
{
    [Fact]
    public void Id_Should_Be_Generated_On_Initialization()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AddDomainEvent_Should_Add_Event_To_Collection()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        // Act
        entity.AddTestEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().ContainSingle();
        entity.DomainEvents.First().Should().Be(domainEvent);
    }
}
