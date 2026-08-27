namespace SystemOrder.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }

    public string CustomerName { get; private set; }

    public string Product { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public decimal Total => Quantity * UnitPrice;

    public Order(
        string customerName,
        string product,
        int quantity,
        decimal unitPrice)
    {
        Id = Guid.NewGuid();

        CustomerName = customerName;
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;

        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string customerName,
        string product,
        int quantity,
        decimal unitPrice)
    {
        CustomerName = customerName;
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}