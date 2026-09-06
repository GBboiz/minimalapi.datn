using MinimalAPI.Domain.Events;
using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
    public StoreId StoreId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public string Code { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public Money SubTotal { get; private set; } = default!;
    public decimal DiscountPercent { get; private set; }
    public Money DiscountAmount { get; private set; } = default!;
    public Money TotalAmount { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;

    private Order() { }

    public static Order Create(
        StoreId storeId,
        CustomerId customerId,
        string code,
        List<OrderItem> items,
        decimal discountPercent = 0,
        decimal discountAmountOverride = 0)
    {
        if (items.Count == 0)
            throw new DomainException("Đơn hàng phải có ít nhất một sản phẩm.");

        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Phần trăm giảm giá phải nằm trong khoảng từ 0% đến 100%.");

        if (discountAmountOverride < 0)
            throw new DomainException("Số tiền giảm giá không được âm.");

        var currency = items[0].UnitPrice.Currency;
        var subTotal = items.Where(i => !i.IsGift).Sum(i => i.TotalPrice);

        decimal calculatedDiscount;
        if (discountAmountOverride > 0)
        {
            calculatedDiscount = Math.Min(subTotal, discountAmountOverride);
            if (discountPercent == 0 && subTotal > 0)
            {
                discountPercent = Math.Round((calculatedDiscount / subTotal) * 100m, 2);
            }
        }
        else
        {
            calculatedDiscount = Math.Round(subTotal * (discountPercent / 100m), 0);
        }

        var finalTotal = Math.Max(0, subTotal - calculatedDiscount);

        var order = new Order
        {
            Id = OrderId.New(),
            StoreId = storeId,
            CustomerId = customerId,
            Code = code.Trim().ToUpperInvariant(),
            Status = OrderStatus.Pending,
            SubTotal = Money.Create(subTotal, currency),
            DiscountPercent = discountPercent,
            DiscountAmount = Money.Create(calculatedDiscount, currency),
            TotalAmount = Money.Create(finalTotal, currency),
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(items);
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.StoreId));

        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Không thể xác nhận đơn hàng đang ở trạng thái {Status}.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderConfirmedEvent(Id, StoreId));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException($"Chỉ có thể hoàn tất đơn hàng đã được xác nhận (trạng thái hiện tại: {Status}).");

        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
            throw new DomainException("Không thể hủy đơn hàng đã hoàn tất.");

        if (Status == OrderStatus.Cancelled)
            return;

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
