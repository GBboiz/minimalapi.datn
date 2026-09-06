using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class Promotion : AggregateRoot<PromotionId>
{
    public StoreId StoreId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Promotion() { }

    public static Promotion Create(
        StoreId storeId,
        string code,
        string name,
        DiscountType type,
        decimal value,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã khuyến mãi không được để trống.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên chương trình khuyến mãi không được để trống.");

        if (value <= 0)
            throw new DomainException("Giá trị khuyến mãi phải lớn hơn 0.");

        if (type == DiscountType.Percentage && value > 100)
            throw new DomainException("Phần trăm giảm giá không được vượt quá 100%.");

        return new Promotion
        {
            Id = PromotionId.New(),
            StoreId = storeId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Type = type,
            Value = value,
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateInfo(string name, DiscountType type, decimal value, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên chương trình khuyến mãi không được để trống.");

        if (value <= 0)
            throw new DomainException("Giá trị khuyến mãi phải lớn hơn 0.");

        if (type == DiscountType.Percentage && value > 100)
            throw new DomainException("Phần trăm giảm giá không được vượt quá 100%.");

        Name = name.Trim();
        Type = type;
        Value = value;
        Description = description?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CalculateDiscount(decimal subTotal)
    {
        if (subTotal <= 0) return 0;

        return Type switch
        {
            DiscountType.Percentage => Math.Round(subTotal * (Value / 100m), 0),
            DiscountType.FixedAmount => Math.Min(subTotal, Value),
            _ => 0
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
