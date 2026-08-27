using SystemOrder.Application.DTOs;

namespace SystemOrder.Application.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetAllAsync();

    Task<OrderResponse> CreateAsync(
        CreateOrderRequest request);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateOrderRequest request);

    Task<bool> DeleteAsync(Guid id);
}