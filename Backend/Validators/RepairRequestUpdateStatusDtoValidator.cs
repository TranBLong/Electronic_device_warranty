using FluentValidation;
using EWarrantySystem.DTOs;

namespace EWarrantySystem.Validators;

public class RepairRequestUpdateStatusDtoValidator : AbstractValidator<RepairRequestUpdateStatusDto>
{
    public RepairRequestUpdateStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => new[] { "Pending", "InProgress", "Completed", "Returned", "Cancelled" }
                .Contains(s))
            .WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x.EstimatedReturnDate)
            .Must(d => d == null || d.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Ngày hẹn trả máy phải từ hôm nay trở đi");

        RuleFor(x => x.TechnicalNote).MaximumLength(500);
    }
}