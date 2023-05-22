using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;

namespace Mercadona.Backend.Swagger
{
    /// <summary>
    /// Permet de modifier les autorisations de route dans SwaggerUI
    /// </summary>
    /// <seealso cref="Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter" />
    [ExcludeFromCodeCoverage]
    public class AuthOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applique l'opération.
        /// </summary>
        /// <param name="operation">L'opération.</param>
        /// <param name="context">Le contexte.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Vérifier si l'action ou le contrôleur nécessite une autorisation
            bool methodHasAuthorize = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any();
            bool methodHasAllowAnomynous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();
            bool controllerHasAuthorize =
                context.MethodInfo.DeclaringType
                    ?.GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Any() == true;
            bool requiresAuth =
                !methodHasAllowAnomynous && (methodHasAuthorize || controllerHasAuthorize);

            if (requiresAuth)
            {
                // Ajouter l'exigence de sécurité pour les routes nécessitant une autorisation
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = JwtBearerDefaults.AuthenticationScheme
                                }
                            },
                            Array.Empty<string>()
                        }
                    }
                };
            }
        }
    }
}
