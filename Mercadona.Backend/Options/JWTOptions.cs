namespace Mercadona.Backend.Options
{
    /// <summary>
    /// Représente les options pour la génération des JWT
    /// </summary>
    public class JWTOptions
    {
        /// <summary>
        /// Définit le public valide pour le jeton.
        /// </summary>
        public string ValidAudience { get; set; } = string.Empty;

        /// <summary>
        /// Définit le fournisseur valide pour le jeton.
        /// </summary>
        public string ValidIssuer { get; set; } = string.Empty;

        /// <summary>
        /// Définit la clé de chiffrement pour le jeton.
        /// </summary>
        public string Secret { get; set; } = string.Empty;
    }
}
