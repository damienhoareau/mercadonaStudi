using FluentValidation;
using Mercadona.Backend;
using Mercadona.Backend.Areas.Identity;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Options;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Swagger;
using Mercadona.Backend.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MimeDetective;
using MudBlazor.Services;
using System.Reflection;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseNpgsql(connectionString)
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// For Identity
builder.Services
    .AddIdentityCore<IdentityUser>(o =>
    {
        o.Stores.MaxLengthForKeys = 128;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Adding Authentication
builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection("JWT"));
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie()
    // Adding Jwt Bearer
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:Secret"]
                        ?? throw new Exception("Impossible de charger JWT:Secret")
                )
            )
        };
    });
builder.Services.AddJwtInSession();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddTransient(provider =>
{
    HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
    ConnectedUser connectedUser = provider.GetRequiredService<ConnectedUser>();
    if (!string.IsNullOrWhiteSpace(connectedUser.AccessToken))
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                connectedUser.AccessToken
            );
    return new AuthenticateClient("https://localhost:44387", httpClient);
});

// Informations sur l'utilisateur connecté
builder.Services.AddScoped<ConnectedUser>();
builder.Services.AddScoped<
    ISecurityStampValidator<IdentityUser>,
    Mercadona.Backend.Areas.Identity.SecurityStampValidator<IdentityUser>
>();
builder.Services.AddScoped<
    AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider<IdentityUser>
>();
builder.Services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(provider =>
{
    // this is safe because
    //     the `RevalidatingIdentityAuthenticationStateProvider` extends the `ServerAuthenticationStateProvider`
    ServerAuthenticationStateProvider authenticationStateProvider =
        (ServerAuthenticationStateProvider)
            provider.GetRequiredService<AuthenticationStateProvider>();
    return authenticationStateProvider;
});

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<ProductValidator>();

// Services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IDiscountedProductService, DiscountedProductService>();

// MIME inspector
builder.Services.AddSingleton(
    new ContentInspectorBuilder()
    {
        Definitions = MimeDetective.Definitions.Default.FileTypes.Images.All()
    }.Build()
);

// Controllers
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddResponseCaching();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Version = "v1",
            Title = "Mercadona API",
            Description =
                "API pour la gestion de produits remisés<br/>Dans le cadre d'une formation Bachelor Développeur C# chez Studi.",
            Contact = new OpenApiContact
            {
                Name = "Damien HOAREAU",
                Email = "damien.hoareau@gmail.com"
            }
        }
    );
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Définir le schéma de sécurité
    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT"
        }
    );
    options.OperationFilter<AuthOperationFilter>();
    options.SupportNonNullableReferenceTypes();
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseResponseCaching();

app.UseStaticFiles();

app.UseRouting();

// API Documentation (avant le pipeline de sécurité afin de permettre les requêtes publiques)
app.UseSwagger();
app.UseSwaggerUI();

app.UseJwtInSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
