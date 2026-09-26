using EWarrantySystem.DTOs;
using FluentValidation;

namespace EWarrantySystem.Validators
{
    public class RepairRequestCreateDtoValidator : AbstractValidator<RepairRequestCreateDto>
    {
        public RepairRequestCreateDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("ProductId phải là số nguyên dương hợp lệ");

            RuleFor(x => x.IssueDescription)
                .NotEmpty()
                .WithMessage("Mô tả sự cố/lỗi thiết bị không được để trống")
                .MaximumLength(500)
                .WithMessage("Mô tả sự cố tối đa 500 ký tự");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("CustomerId phải là số nguyên dương hợp lệ");

            RuleFor(x => x.ReceptionistId)
                .GreaterThan(0)
                .When(x => x.ReceptionistId.HasValue)
                .WithMessage("ReceptionistId phải là số nguyên dương hợp lệ");
        }
    }
}
