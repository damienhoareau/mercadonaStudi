using FluentAssertions;
using Mercadona.Backend.Helpers;
using MudBlazor;

namespace Mercadona.Tests.Options;

public class SnackbarHelperTests
{
    [Fact]
    public void Defualts_ShouldReturnStringEmpty()
    {
        // Arrange

        // Act
        Dictionary<string, Snackbar> shownSnackbars = SnackbarHelper.ShownSnackbars;

        // Assert
        shownSnackbars.Should().BeEmpty();
    }
}
