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
        List<OrderItem> items)
    {
        if (items.Count == 0)
            throw new DomainException("Đơn hàng phải có ít nhất một sản phẩm.");

        var currency = items[0].UnitPrice.Currency;
        var total = items.Sum(i => i.TotalPrice);

        var order = new Order
        {
            Id = OrderId.New(),
            StoreId = storeId,
            CustomerId = customerId,
            Code = code.Trim().ToUpperInvariant(),
            Status = OrderStatus.Pending,
            TotalAmount = Money.Create(total, currency),
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
