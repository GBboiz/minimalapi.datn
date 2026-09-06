using MinimalAPI.Domain.Events;
using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Aggregate Root — Sản phẩm.
/// Mọi thay đổi trạng thái phải qua method, KHÔNG public setter.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public StoreId StoreId { get; private set; }
    public string Sku { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int ForecastStock => Math.Max(0, StockQuantity - ReservedQuantity);
    public ProductName Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public CategoryId CategoryId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public ProductId? GiftProductId { get; private set; }
    public string? GiftProductName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core cần parameterless constructor
    private Product() { }

    public static Product Create(
        StoreId storeId,
        string sku,
        int stockQuantity,
        ProductName name,
        Money price,
        CategoryId categoryId,
        string? description,
        ProductId? giftProductId = null,
        string? giftProductName = null)
    {
        var product = new Product
        {
            Id = ProductId.New(),
            StoreId = storeId,
            Sku = sku.Trim().ToUpperInvariant(),
            StockQuantity = stockQuantity,
            Name = name,
            Price = price,
            CategoryId = categoryId,
            Description = description?.Trim(),
            GiftProductId = giftProductId,
            GiftProductName = giftProductName?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id));

        return product;
    }

    public void SetGiftProduct(ProductId? giftProductId, string? giftProductName)
    {
        GiftProductId = giftProductId;
        GiftProductName = giftProductName?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string sku, ProductName name, CategoryId categoryId, string? description, ProductId? giftProductId = null, string? giftProductName = null)
    {
        Sku = sku.Trim().ToUpperInvariant();
        Name = name;
        CategoryId = categoryId;
        Description = description?.Trim();
        GiftProductId = giftProductId;
        GiftProductName = giftProductName?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng giữ chỗ phải lớn hơn 0.");

        if (ForecastStock < quantity)
            throw new DomainException($"Sản phẩm '{Name.Value}' không đủ tồn kho dự báo (khả dụng: {ForecastStock}, cần: {quantity}).");

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmReservedStock(int quantity)
    {
        if (quantity <= 0) return;

        if (StockQuantity < quantity)
            throw new DomainException($"Sản phẩm '{Name.Value}' không đủ tồn kho thực tế (còn: {StockQuantity}, cần: {quantity}).");

        StockQuantity -= quantity;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservedStock(int quantity)
    {
        if (quantity <= 0) return;

        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustStock(int quantity)
    {
        if (StockQuantity + quantity < 0)
            throw new DomainException("Tồn kho không đủ.");
        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Số lượng tồn kho không được âm.");
        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(Money newPrice)
    {
        if (Price == newPrice) return; // Không raise event nếu giá không đổi

        var oldPrice = Price;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
