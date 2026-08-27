namespace SystemOrder.Application.DTOs;

public record UpdateOrderRequest(
    string CustomerName,
    string Product,
    int Quantity,
    decimal UnitPrice
);