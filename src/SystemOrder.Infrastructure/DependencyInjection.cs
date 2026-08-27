using Microsoft.Extensions.DependencyInjection;
using SystemOrder.Application.Interfaces;
using SystemOrder.Infrastructure.Cache;
using SystemOrder.Infrastructure.Repositories;

namespace SystemOrder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IOrderRepository,
            InMemoryOrderRepository>();

        services.AddSingleton<IOrderCache,
            OrderMemoryCache>();

        return services;
    }
}