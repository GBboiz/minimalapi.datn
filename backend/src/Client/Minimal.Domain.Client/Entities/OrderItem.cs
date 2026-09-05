using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Entities;

public sealed class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public Money UnitPrice { get; private set; } = default!;
    public int Quantity { get; private set; }

    public decimal TotalPrice => UnitPrice.Amount * Quantity;

    private OrderItem() { }

    public static OrderItem Create(
        OrderId orderId,
        ProductId productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng sản phẩm trong đơn hàng phải lớn hơn 0.");

        return new OrderItem
        {
            Id = OrderItemId.New(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
