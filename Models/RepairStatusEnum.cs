namespace EWarrantySystem.Models
{
    public enum RepairStatusEnum
    {
        Pending = 0,     // Chờ tiếp nhận / Chờ phân công[cite: 2]
        InProgress = 1,  // Đang tiến hành sửa chữa[cite: 2]
        Completed = 2,   // Đã hoàn thành sửa chữa[cite: 2]
        Returned = 3,    // Đã bàn giao lại cho khách hàng[cite: 2, 3]
        Cancelled = 4    // Đã hủy / Từ chối bảo hành
    }
}