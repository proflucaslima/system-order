using SystemOrder.Application.DTOs;
using SystemOrder.Application.Interfaces;
using SystemOrder.Domain.Entities;

namespace SystemOrder.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IOrderCache _cache;

    public OrderService(
        IOrderRepository repository,
        IOrderCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync()
    {
        var cachedOrders = _cache.Get();

        if (cachedOrders is not null)
        {
            Console.WriteLine("Pedidos encontrados no CACHE.");

            return cachedOrders.Select(MapToResponse);
        }

        Console.WriteLine("Buscando pedidos no REPOSITORY.");

        var orders = await _repository.GetAllAsync();

        _cache.Set(orders);

        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request)
    {
        Validate(
            request.CustomerName,
            request.Product,
            request.Quantity,
            request.UnitPrice);

        var order = new Order(
            request.CustomerName,
            request.Product,
            request.Quantity,
            request.UnitPrice);

        await _repository.AddAsync(order);

        // O cache ficou desatualizado.
        _cache.Remove();

        return MapToResponse(order);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateOrderRequest request)
    {
        Validate(
            request.CustomerName,
            request.Product,
            request.Quantity,
            request.UnitPrice);

        var order = await _repository.GetByIdAsync(id);

        if (order is null)
        {
            return false;
        }

        order.Update(
            request.CustomerName,
            request.Product,
            request.Quantity,
            request.UnitPrice);

        await _repository.UpdateAsync(order);

        // O cache ficou desatualizado.
        _cache.Remove();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);

        if (order is null)
        {
            return false;
        }

        await _repository.DeleteAsync(id);

        // O cache ficou desatualizado.
        _cache.Remove();

        return true;
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.Product,
            order.Quantity,
            order.UnitPrice,
            order.Total,
            order.CreatedAt);
    }

    private static void Validate(
        string customerName,
        string product,
        int quantity,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException(
                "Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentException(
                "Product is required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentException(
                "Unit price must be greater than zero.");
        }
    }
}