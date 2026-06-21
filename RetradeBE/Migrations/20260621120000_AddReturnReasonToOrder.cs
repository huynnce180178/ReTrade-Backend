using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RetradeBE.Data;

#nullable disable

namespace RetradeBE.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621120000_AddReturnReasonToOrder")]
    public partial class AddReturnReasonToOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "return_reason",
                table: "Order",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "return_reason",
                table: "Order");
        }
    }
}
