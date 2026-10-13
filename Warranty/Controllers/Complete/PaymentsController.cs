using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices/{invoiceId:int}/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Receptionist))]
    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Create(
        int invoiceId,
        PaymentCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        var payment = await paymentService.CreateAsync(invoiceId, request, userId, role, cancellationToken);
        return CreatedAtAction(nameof(GetByInvoiceId), new { invoiceId }, payment);
    }

    [Authorize(Roles = "Admin,Manager,Receptionist,Customer")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentResponseDto>>> GetByInvoiceId(
        int invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await paymentService.GetByInvoiceIdAsync(invoiceId, userId, role, cancellationToken));
    }

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
