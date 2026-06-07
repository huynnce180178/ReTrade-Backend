using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPasswordSetColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_password_set",
                table: "account",
                type: "boolean",
                nullable: true,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_password_set",
                table: "account");
        }
    }
}
