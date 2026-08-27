namespace SystemOrder.Application.DTOs;

public record CreateOrderRequest(
    string CustomerName,
    string Product,
    int Quantity,
    decimal UnitPrice
);