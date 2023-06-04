using FluentAssertions;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services.Interfaces;

namespace Mercadona.Tests.Services;

public class ConnectedUserProviderTests
{
    [Fact]
    public void ConnectedUser_Null_ShouldReturnNull()
    {
        // Arrange
        IConnectedUserProvider connectedUserProvider = new ConnectedUserProvider();

        // Act
        ConnectedUser? result = connectedUserProvider.ConnectedUser;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConnectedUser_NotNull_ShouldReturnNotNull()
    {
        // Arrange
        IConnectedUserProvider connectedUserProvider = new ConnectedUserProvider();
        ConnectedUser expectedConnectedUser =
            new()
            {
                UserName = "toto@toto.fr",
                RefreshToken = "refreshToken",
                AccessToken = "accessToken"
            };
        connectedUserProvider.ConnectedUser = expectedConnectedUser;

        // Act
        ConnectedUser? result = connectedUserProvider.ConnectedUser;

        // Assert
        result.Should().Be(expectedConnectedUser);
    }
}
