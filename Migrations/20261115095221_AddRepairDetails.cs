using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warranty.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceCondition",
                table: "RepairRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisNote",
                table: "RepairRequests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedCompletionDate",
                table: "RepairRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "RepairRequests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceCondition",
                table: "RepairRequests");

            migrationBuilder.DropColumn(
                name: "DiagnosisNote",
                table: "RepairRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletionDate",
                table: "RepairRequests");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "RepairRequests");
        }
    }
}
