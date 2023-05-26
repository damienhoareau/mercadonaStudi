using Microsoft.Extensions.DependencyInjection;

namespace Mercadona.Tests.Fixtures;

public class ServiceProviderFixture : IServiceProvider
{
    private IServiceCollection? _services;
    private IServiceProvider? _serviceProvider;

    public ServiceProviderFixture()
    {
        Reconfigure(null);
    }

    public object? GetService(Type serviceType) => _serviceProvider?.GetService(serviceType);

    private IServiceCollection PrivateConfigure()
    {
        _services = new ServiceCollection();

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

        return _serviceProvider;
    }
}
