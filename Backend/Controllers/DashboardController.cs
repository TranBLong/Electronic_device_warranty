using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Data;
using EWarrantySystem.Models;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/dashboard/summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var now = DateTime.UtcNow;
            var in30Days = now.AddDays(30);

            var repairByStatus = await _context.RepairRequests
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var pendingOrInProgress = await _context.RepairRequests
                .CountAsync(r => r.Status == RepairStatusEnum.Pending
                              || r.Status == RepairStatusEnum.InProgress);

            var expiringWarranties = await _context.WarrantyCards
                .CountAsync(w => w.IsActive
                              && w.EndDate >= now
                              && w.EndDate <= in30Days);

            var expiredWarranties = await _context.WarrantyCards
                .CountAsync(w => w.IsActive && w.EndDate < now);

            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users
                .CountAsync(u => u.Role == "Customer" && u.IsActive);

            return Ok(new
            {
                repairRequestsByStatus = repairByStatus,
                activeRepairCount = pendingOrInProgress,
                warrantiesExpiringIn30Days = expiringWarranties,
                expiredWarranties,
                totalProducts,
                totalCustomers
            });
        }

        /// <summary>
        /// GET /api/dashboard/expiring-warranties?days=30
        /// </summary>
        [HttpGet("expiring-warranties")]
        public async Task<IActionResult> GetExpiringWarranties([FromQuery] int days = 30)
        {
            if (days < 1 || days > 365) days = 30;
            var now = DateTime.UtcNow;
            var until = now.AddDays(days);

            var list = await _context.WarrantyCards
                .Include(w => w.Product)!.ThenInclude(p => p!.Customer)
                .Where(w => w.IsActive && w.EndDate >= now && w.EndDate <= until)
                .OrderBy(w => w.EndDate)
                .Select(w => new
                {
                    w.Id,
                    w.CardCode,
                    w.EndDate,
                    DaysRemaining = (int)(w.EndDate - now).TotalDays,
                    ProductId = w.ProductId,
                    ProductName = w.Product!.Name,
                    SerialNumber = w.Product.SerialNumber,
                    CustomerName = w.Product.Customer!.FullName,
                    CustomerPhone = w.Product.Customer.PhoneNumber
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}