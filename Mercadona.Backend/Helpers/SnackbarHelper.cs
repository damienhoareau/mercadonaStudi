using MudBlazor;

namespace Mercadona.Backend.Helpers;

/// <summary>
/// Classe stockant une liste de composants Snackbar identifiés par une clé
/// </summary>
public static class SnackbarHelper
{
    /// <value>
    /// La liste des composants Snackbar.
    /// </value>
    public static Dictionary<string, Snackbar> ShownSnackbars { get; } =
        new Dictionary<string, Snackbar>();
}
