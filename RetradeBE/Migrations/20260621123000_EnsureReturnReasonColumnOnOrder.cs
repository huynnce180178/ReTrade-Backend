using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RetradeBE.Data;

#nullable disable

namespace RetradeBE.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621123000_EnsureReturnReasonColumnOnOrder")]
    public partial class EnsureReturnReasonColumnOnOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Order"
                ADD COLUMN IF NOT EXISTS return_reason text;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Order"
                DROP COLUMN IF EXISTS return_reason;
                """);
        }
    }
}
