using Mercadona.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using System.Data.Common;

namespace Mercadona.Tests.Fixtures
{
    public class ApplicationDbContextFixture : DatabaseServerFixture, IServiceProvider
    {
        private IServiceCollection? _services;
        private IServiceProvider? _serviceProvider;
        private Respawner? _respawner;

        public ApplicationDbContextFixture() : base()
        {
            Reconfigure(null);
        }

        public object? GetService(Type serviceType) => _serviceProvider?.GetService(serviceType);

        private IServiceCollection PrivateConfigure()
        {
            _services = new ServiceCollection();
            _services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseNpgsql(
                        $"Server=localhost;Port={PgPort};User Id=postgres;Password=test;Database=postgres"
                    )
            );

            return _services;
        }

        public IServiceProvider Reconfigure(
            Func<IServiceCollection, IServiceCollection>? additionalConfiguration
        )
        {
            _services = PrivateConfigure();
            if (additionalConfiguration != null)
                _services = additionalConfiguration(_services);

            _serviceProvider = _services.BuildServiceProvider();

            ApplicationDbContext dbContext =
                _serviceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
            DbConnection conn = _serviceProvider
                .GetRequiredService<ApplicationDbContext>()
                .Database.GetDbConnection();
            conn.Open();

            _respawner = Respawner
                .CreateAsync(
                    conn,
                    new RespawnerOptions
                    {
                        SchemasToInclude = new[] { "public" },
                        DbAdapter = DbAdapter.Postgres
                    }
                )
                .GetAwaiter()
                .GetResult();

            return _serviceProvider;
        }

        public async Task ResetDbAsync()
        {
            if (_respawner != null && _serviceProvider != null)
            {
                DbConnection conn = _serviceProvider
                    .GetRequiredService<ApplicationDbContext>()
                    .Database.GetDbConnection();
                await _respawner.ResetAsync(conn);
            }
        }
    }
}
