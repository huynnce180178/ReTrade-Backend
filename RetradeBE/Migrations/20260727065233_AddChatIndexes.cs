using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddChatIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_chat_room_id",
                table: "chat",
                newName: "idx_chat_room_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_created_at",
                table: "chat",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_chat_is_read",
                table: "chat",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "idx_chat_room_created_at",
                table: "chat",
                columns: new[] { "room_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_chat_room_is_read",
                table: "chat",
                columns: new[] { "room_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_chat_created_at",
                table: "chat");

            migrationBuilder.DropIndex(
                name: "idx_chat_is_read",
                table: "chat");

            migrationBuilder.DropIndex(
                name: "idx_chat_room_created_at",
                table: "chat");

            migrationBuilder.DropIndex(
                name: "idx_chat_room_is_read",
                table: "chat");

            migrationBuilder.RenameIndex(
                name: "idx_chat_room_id",
                table: "chat",
                newName: "IX_chat_room_id");
        }
    }
}
