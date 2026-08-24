using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Project_Final_BE.Data;

#nullable disable

namespace Project_Final_BE.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260824120000_AddReturnRequestWorkflow")]
    public partial class AddReturnRequestWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAt",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_Status_ReturnRequestedAt",
                table: "BorrowRecords",
                columns: new[] { "Status", "ReturnRequestedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_Status_ReturnRequestedAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAt",
                table: "BorrowRecords");
        }
    }
}
