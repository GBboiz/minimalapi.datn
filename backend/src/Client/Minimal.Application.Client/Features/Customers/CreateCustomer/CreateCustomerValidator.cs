using FluentValidation;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên khách hàng không được để trống.")
            .MaximumLength(150).WithMessage("Tên khách hàng tối đa 150 ký tự.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email không hợp lệ.")
            .MaximumLength(256).WithMessage("Email tối đa 256 ký tự.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự.");
    }
}
