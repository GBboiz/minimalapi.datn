using FluentValidation;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Khách hàng không được để trống.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Đơn hàng phải có ít nhất một sản phẩm.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Mã sản phẩm không được để trống.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng sản phẩm phải lớn hơn 0.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).When(i => i.UnitPrice.HasValue).WithMessage("Đơn giá không được âm.");
        });
    }
}
