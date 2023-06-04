using FluentAssertions;
using Mercadona.Backend.Models;
using Mercadona.Backend.Options;
using Mercadona.Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercadona.Tests.Options;

public class JwtOptionsTests
{
    [Fact]
    public void Defualts_ShouldReturnStringEmpty()
    {
        // Arrange

        // Act
        JwtOptions jwtOptions = new();

        // Assert
        jwtOptions.ValidAudience.Should().BeEmpty();
        jwtOptions.ValidIssuer.Should().BeEmpty();
        jwtOptions.Secret.Should().BeEmpty();
    }

    [Fact]
    public void ConnectedUser_NotNull_ShouldReturnNotNull()
    {
        // Arrange
        string expectedValidAudience = "audience";
        string expectedValidIssuer = "issuer";
        string expectedSecret = "secret";

        // Act
        JwtOptions jwtOptions =
            new()
            {
                ValidAudience = expectedValidAudience,
                ValidIssuer = expectedValidIssuer,
                Secret = expectedSecret
            };

        // Assert
        jwtOptions.ValidAudience.Should().Be(expectedValidAudience);
        jwtOptions.ValidIssuer.Should().Be(expectedValidIssuer);
        jwtOptions.Secret.Should().Be(expectedSecret);
    }
}
