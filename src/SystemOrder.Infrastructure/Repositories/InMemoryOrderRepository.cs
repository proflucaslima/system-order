using SystemOrder.Application.Interfaces;
using SystemOrder.Domain.Entities;

namespace SystemOrder.Infrastructure.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private static readonly List<Order> Orders = [];

    public Task<IEnumerable<Order>> GetAllAsync()
    {
        return Task.FromResult(
            Orders.AsEnumerable());
    }

    public Task<Order?> GetByIdAsync(Guid id)
    {
        var order = Orders
            .FirstOrDefault(x => x.Id == id);

        return Task.FromResult(order);
    }

    public Task AddAsync(Order order)
    {
        Orders.Add(order);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order)
    {
        // Como o objeto está em memória,
        // ele já foi alterado.

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var order = Orders
            .FirstOrDefault(x => x.Id == id);

        if (order is not null)
        {
            Orders.Remove(order);
        }

        return Task.CompletedTask;
    }
}