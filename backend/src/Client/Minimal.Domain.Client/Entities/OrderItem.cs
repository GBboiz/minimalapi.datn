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
    public bool IsGift { get; private set; }

    public decimal TotalPrice => UnitPrice.Amount * Quantity;

    private OrderItem() { }

    public static OrderItem Create(
        OrderId orderId,
        ProductId productId,
        string productName,
        Money unitPrice,
        int quantity,
        bool isGift = false)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng sản phẩm trong đơn hàng phải lớn hơn 0.");

        var finalPrice = isGift ? Money.Create(0, unitPrice.Currency) : unitPrice;

        return new OrderItem
        {
            Id = OrderItemId.New(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = isGift && !productName.Contains("[Quà tặng]") ? $"[Quà tặng] {productName.Trim()}" : productName.Trim(),
            UnitPrice = finalPrice,
            Quantity = quantity,
            IsGift = isGift
        };
    }
}
