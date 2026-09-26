using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Orders.UpdateOrder;

public sealed class UpdateOrderHandler(
    IOrderRepository orderRepo,
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var order = await orderRepo.GetByIdAsync(new OrderId(request.Id), storeId, ct);
        if (order is null)
            return Result<Guid>.Failure("Đơn hàng không tồn tại.");

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            return Result<Guid>.Failure("Không thể chỉnh sửa đơn hàng đã hoàn tất hoặc đã hủy.");

        // 1. Cập nhật thông tin khách hàng nếu có
        var customer = await customerRepo.GetByIdAsync(order.CustomerId, storeId, ct);
        if (customer is not null)
        {
            var name = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim() : customer.Name;
            var phone = !string.IsNullOrWhiteSpace(request.CustomerPhone) ? request.CustomerPhone.Trim() : customer.Phone;
            var address = request.CustomerAddress != null ? request.CustomerAddress.Trim() : customer.Address;
            customer.UpdateInfo(name, phone, customer.Email, address);
        }

        // 2. Cập nhật danh sách sản phẩm nếu có
        if (request.Items is not null && request.Items.Count > 0)
        {
            if (order.Status != OrderStatus.Pending)
                return Result<Guid>.Failure("Chỉ có thể chỉnh sửa sản phẩm khi đơn hàng ở trạng thái Chờ duyệt (Pending).");

            // 2.1. Giải phóng tồn kho dự báo của các sản phẩm cũ
            var oldGrouped = order.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .ToList();

            foreach (var item in oldGrouped)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
                product?.ReleaseReservedStock(item.TotalQuantity);
            }

            // 2.2. Tạo danh sách sản phẩm mới
            var newOrderItems = new List<OrderItem>();
            foreach (var itemReq in request.Items)
            {
                var product = await productRepo.GetByIdAsync(new ProductId(itemReq.ProductId), storeId, ct);
                if (product is null)
                    return Result<Guid>.Failure($"Sản phẩm với Id {itemReq.ProductId} không tồn tại.");

                if (!product.IsActive)
                    return Result<Guid>.Failure($"Sản phẩm '{product.Name.Value}' đã ngưng hoạt động.");

                var itemPrice = itemReq.UnitPrice.HasValue && itemReq.UnitPrice.Value >= 0 && !itemReq.IsGift
                    ? Money.Create(itemReq.UnitPrice.Value, product.Price.Currency)
                    : product.Price;

                newOrderItems.Add(OrderItem.Create(
                    order.Id,
                    product.Id,
                    product.Name.Value,
                    itemPrice,
                    itemReq.Quantity,
                    isGift: itemReq.IsGift));
            }

            // 2.3. Kiểm tra và giữ chỗ tồn kho dự báo cho các sản phẩm mới
            var newGrouped = newOrderItems
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .ToList();

            foreach (var nq in newGrouped)
            {
                var product = await productRepo.GetByIdAsync(nq.ProductId, storeId, ct);
                if (product is null)
                    return Result<Guid>.Failure("Sản phẩm không tồn tại.");

                if (product.ForecastStock < nq.TotalQuantity)
                {
                    return Result<Guid>.Failure(
                        $"Sản phẩm '{product.Name.Value}' không đủ tồn kho dự báo (khả dụng: {product.ForecastStock}, cần: {nq.TotalQuantity}).");
                }

                product.ReserveStock(nq.TotalQuantity);
            }

            // 2.4. Cập nhật chi tiết đơn hàng
            var discountPct = request.DiscountPercent ?? order.DiscountPercent;
            var discountAmt = request.DiscountAmount.HasValue
                ? request.DiscountAmount.Value
                : (request.DiscountPercent.HasValue ? 0 : order.DiscountAmount.Amount);
            order.UpdateOrderDetails(newOrderItems, discountPct, discountAmt);
        }
        else if (request.DiscountPercent.HasValue || request.DiscountAmount.HasValue)
        {
            if (order.Status == OrderStatus.Pending)
            {
                var discountPct = request.DiscountPercent ?? order.DiscountPercent;
                var discountAmt = request.DiscountAmount.HasValue
                    ? request.DiscountAmount.Value
                    : (request.DiscountPercent.HasValue ? 0 : order.DiscountAmount.Amount);
                order.UpdateDiscount(discountPct, discountAmt);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
