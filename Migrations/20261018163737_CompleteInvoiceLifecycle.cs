using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warranty.Migrations
{
    /// <inheritdoc />
    public partial class CompleteInvoiceLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_RepairRequestId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledById",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CancelledById",
                table: "Invoices",
                column: "CancelledById");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RepairRequestId",
                table: "Invoices",
                column: "RepairRequestId",
                unique: true,
                filter: "[Status] <> 'Cancelled'");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CancelledById",
                table: "Invoices",
                column: "CancelledById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CancelledById",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CancelledById",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_RepairRequestId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledById",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RepairRequestId",
                table: "Invoices",
                column: "RepairRequestId",
                unique: true);
        }
    }
}
