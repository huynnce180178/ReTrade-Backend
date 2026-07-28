using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "review",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "review");

        }
    }
}
