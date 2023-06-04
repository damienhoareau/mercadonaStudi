using FluentAssertions;
using Mercadona.Backend.Pages;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Diagnostics;

namespace Mercadona.Tests.Data;

public class ErrorModelTests
{
    [Fact]
    public void RequestId_Default()
    {
        // Arrange
        ErrorModel model = new();

        // Act

        // Assert
        model.RequestId.Should().BeNull();
        model.ShowRequestId.Should().BeFalse();
    }

    [Fact]
    public void RequestId_WithValue()
    {
        // Arrange
        ErrorModel model = new() { RequestId = "myRequest" };

        // Act

        // Assert
        model.RequestId.Should().Be("myRequest");
        model.ShowRequestId.Should().BeTrue();
    }

    [Fact]
    public void OnGet_WithActivity()
    {
        // Arrange
        ErrorModel model = new();
        Activity.Current = new Activity("myActivity").Start();
        string expectedRequestId = Activity.Current.Id!;

        // Act
        model.OnGet();

        // Assert
        model.RequestId.Should().NotBeNull();
        model.RequestId.Should().Be(expectedRequestId);

        // Clean
        Activity.Current.Stop();
    }

    [Fact]
    public void OnGet_WithoutActivity()
    {
        // Arrange
        Mock<HttpContext> mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.SetupGet(x => x.TraceIdentifier).Returns("test");
        ErrorModel model = new() { PageContext = new() { HttpContext = mockHttpContext.Object } };

        // Act
        model.OnGet();

        // Assert
        model.RequestId.Should().NotBeNull();
        model.RequestId.Should().Be("test");
    }
}
