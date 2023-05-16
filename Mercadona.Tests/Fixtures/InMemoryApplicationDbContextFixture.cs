using Mercadona.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mercadona.Tests.Fixtures
{
    public class InMemoryApplicationDbContextFixture : IServiceProvider
    {
        private IServiceCollection? _services;
        private IServiceProvider? _serviceProvider;

        public InMemoryApplicationDbContextFixture() : base()
        {
            Reconfigure(null);
        }

        public object? GetService(Type serviceType) => _serviceProvider?.GetService(serviceType);

        private IServiceCollection PrivateConfigure()
        {
            _services = new ServiceCollection();
            _services.AddDbContextFactory<ApplicationDbContext>(
                options => options.UseInMemoryDatabase(Guid.NewGuid().ToString())
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

            return _serviceProvider;
        }

        public async Task ResetDbAsync()
        {
            if (_serviceProvider != null)
            {
                ApplicationDbContext dbContext =
                    _serviceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
