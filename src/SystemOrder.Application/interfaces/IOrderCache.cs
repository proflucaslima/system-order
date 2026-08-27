using SystemOrder.Domain.Entities;

namespace SystemOrder.Application.Interfaces;

public interface IOrderCache
{
    IEnumerable<Order>? Get();

    void Set(IEnumerable<Order> orders);

    void Remove();
}