using FluentAssertions;
using HttpContextMoq;
using HttpContextMoq.Extensions;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Mercadona.Tests.Security;

public class AutoValidateAntiforgeryTokenFilterTests
{
    [Fact]
    public void Attribute_Constructor()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(new Mock<IAntiforgery>().Object);
        services.AddSingleton<AuthAutoValidateAntiforgeryTokenFilter>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        AuthAutoValidateAntiforgeryTokenAttribute attribute = new();
        IFilterMetadata instance = attribute.CreateInstance(serviceProvider);

        // Assert
        attribute.Order.Should().Be(1000);
        attribute.IsReusable.Should().BeTrue();
        instance.Should().BeOfType<AuthAutoValidateAntiforgeryTokenFilter>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_IsNotEffectivePolicy_Async()
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        mockAntiforgery.Invocations.Should().BeEmpty();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldNotValidate_Async()
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(false);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        mockAntiforgery.Invocations.Should().BeEmpty();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldValidate_Async()
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(true);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        mockAntiforgery.Invocations.Should().ContainSingle();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_AntiforgeryException_Async()
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        mockAntiforgery
            .Setup(_ => _.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new Exception());
        HttpContextMock httpContext = new();
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(true);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        mockAntiforgery.Invocations.Should().ContainSingle();
        context.Result.Should().BeOfType<AntiforgeryValidationFailedResult>();
    }

    [Fact]
    public void ShouldValidate_ContainsAuthorizationHeader_ShouldReturnFalse()
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        httpContext.SetupRequestHeaders(
            new Dictionary<string, StringValues>() { ["Authorization"] = "accessToken" }
        );
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(true);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        bool result = filter.CallShouldValidate(context);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("TRACE")]
    [InlineData("OPTIONS")]
    public void ShouldValidate_NotSupportedMethod_ShouldReturnFalse(string method)
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        httpContext.SetupRequestMethod(method);
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(true);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        bool result = filter.CallShouldValidate(context);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("CONNECT")]
    public void ShouldValidate_SupportedMethod_ShouldReturnTrue(string method)
    {
        // Arrange
        Mock<IAntiforgery> mockAntiforgery = new();
        HttpContextMock httpContext = new();
        httpContext.SetupRequestMethod(method);
        AuthAutoValidateAntiforgeryTokenFilterMock filter = new(mockAntiforgery.Object);
        filter.SetShouldValidate(true);
        ActionContext actionContext =
            new(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );
        AuthorizationFilterContext context =
            new(actionContext, new List<IFilterMetadata>() { filter });

        // Act
        bool result = filter.CallShouldValidate(context);

        // Assert
        result.Should().BeTrue();
    }
}
