using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Receptionist))]
    [HttpPost("repair-requests/{repairRequestId:int}/invoice")]
    public async Task<ActionResult<InvoiceResponseDto>> Create(
        int repairRequestId,
        InvoiceCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        var invoice = await invoiceService.CreateAsync(repairRequestId, userId, role, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [Authorize(Roles = "Admin,Manager,Receptionist,Customer")]
    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<InvoiceResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await invoiceService.GetByIdAsync(id, userId, role, cancellationToken));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist,Customer")]
    [HttpGet("repair-requests/{repairRequestId:int}/invoice")]
    public async Task<ActionResult<InvoiceResponseDto>> GetByRepairRequestId(
        int repairRequestId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await invoiceService.GetByRepairRequestIdAsync(repairRequestId, userId, role, cancellationToken));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResponseDto<InvoiceListResponseDto>>> GetAll(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await invoiceService.GetAllAsync(status, page, pageSize, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("invoices/my")]
    public async Task<ActionResult<PagedResponseDto<InvoiceListResponseDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        return Ok(await invoiceService.GetMineAsync(customerId, page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "Receptionist,Manager")]
    [HttpPatch("invoices/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserRole(out var role))
        {
            return Unauthorized();
        }

        await invoiceService.CancelAsync(id, role, cancellationToken);
        return NoContent();
    }

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
