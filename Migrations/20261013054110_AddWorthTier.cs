using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warranty.Migrations
{
    /// <inheritdoc />
    public partial class AddWorthTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepairRequestParts_RepairRequestId_PartId",
                table: "RepairRequestParts");

            migrationBuilder.RenameColumn(
                name: "AddedAt",
                table: "RepairRequestParts",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Parts",
                newName: "PartNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Parts_Code",
                table: "Parts",
                newName: "IX_Parts_PartNumber");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Parts",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Parts])
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Name] = N'Uncategorized')
                    BEGIN
                        INSERT INTO [Categories] ([Name], [Description], [IsActive], [CreatedAt])
                        VALUES (N'Uncategorized', NULL, 1, SYSUTCDATETIME());
                    END;

                    UPDATE [Parts]
                    SET [CategoryId] = (SELECT [Id] FROM [Categories] WHERE [Name] = N'Uncategorized')
                    WHERE [CategoryId] IS NULL;
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Parts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequestParts_RepairRequestId_PartId",
                table: "RepairRequestParts",
                columns: new[] { "RepairRequestId", "PartId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parts_CategoryId",
                table: "Parts",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parts_Categories_CategoryId",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_RepairRequestParts_RepairRequestId_PartId",
                table: "RepairRequestParts");

            migrationBuilder.DropIndex(
                name: "IX_Parts_CategoryId",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Parts");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                table: "RepairRequestParts",
                newName: "AddedAt");

            migrationBuilder.RenameColumn(
                name: "PartNumber",
                table: "Parts",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Parts_PartNumber",
                table: "Parts",
                newName: "IX_Parts_Code");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequestParts_RepairRequestId_PartId",
                table: "RepairRequestParts",
                columns: new[] { "RepairRequestId", "PartId" },
                unique: true);
        }
    }
}
