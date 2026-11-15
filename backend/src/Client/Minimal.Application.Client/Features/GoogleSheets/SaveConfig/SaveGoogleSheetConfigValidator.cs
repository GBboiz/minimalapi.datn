using FluentValidation;

namespace MinimalAPI.Application.Features.GoogleSheets.SaveConfig;

public sealed class SaveGoogleSheetConfigValidator : AbstractValidator<SaveGoogleSheetConfigCommand>
{
    public SaveGoogleSheetConfigValidator()
    {
        RuleFor(x => x.SpreadsheetId)
            .NotEmpty().WithMessage("Spreadsheet ID không được để trống.")
            .MaximumLength(256).WithMessage("Spreadsheet ID tối đa 256 ký tự.");

        RuleFor(x => x.SheetName)
            .MaximumLength(100).WithMessage("Tên Sheet tab tối đa 100 ký tự.");
    }
}
