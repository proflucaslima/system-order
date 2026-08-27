namespace SystemOrder.Application.DTOs;

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string Product,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    DateTime CreatedAt
);