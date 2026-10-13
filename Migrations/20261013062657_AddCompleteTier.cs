using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warranty.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "ServiceCenters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IssuedById",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCenters_ManagerId",
                table: "ServiceCenters",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssuedById",
                table: "Invoices",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_IssuedById",
                table: "Invoices",
                column: "IssuedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceCenters_Users_ManagerId",
                table: "ServiceCenters",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_IssuedById",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCenters_Users_ManagerId",
                table: "ServiceCenters");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCenters_ManagerId",
                table: "ServiceCenters");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_IssuedById",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "ServiceCenters");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
