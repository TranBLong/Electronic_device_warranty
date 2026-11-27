namespace Warranty.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";
    public const string Technician = "Technician";
    public const string Customer = "Customer";
    public const string AdminOrManager = "Admin,Manager";
    public const string Staff = "Admin,Manager,Receptionist";
    public const string InternalStaff = "Admin,Manager,Receptionist,Technician";
}
