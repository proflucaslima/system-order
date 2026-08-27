using SystemOrder.Domain.Entities;

namespace SystemOrder.Application.Interfaces;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync();

    Task<Order?> GetByIdAsync(Guid id);

    Task AddAsync(Order order);

    Task UpdateAsync(Order order);

    Task DeleteAsync(Guid id);
}