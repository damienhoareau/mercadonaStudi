namespace Mercadona.Backend.Models
{
    /// <summary>
    /// Représente le résultat d'une connexion réussie
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// Token JWT d'autorisation
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Date d'expiration du token
        /// </summary>
        public DateTime Expiration { get; set; }
    }
}
