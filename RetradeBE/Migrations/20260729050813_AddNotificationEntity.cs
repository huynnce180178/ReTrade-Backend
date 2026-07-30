using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_user_id",
                table: "notification");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "notification",
                newName: "is_deleted");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                table: "notification",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_user_created",
                table: "notification",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_user_is_read",
                table: "notification",
                columns: new[] { "user_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_notification_user_created",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "idx_notification_user_is_read",
                table: "notification");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "notification",
                newName: "IsDeleted");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "notification",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_id",
                table: "notification",
                column: "user_id");
        }
    }
}
