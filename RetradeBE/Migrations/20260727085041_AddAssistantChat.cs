using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_session",
                columns: table => new
                {
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_session_pkey", x => x.session_id);
                    table.ForeignKey(
                        name: "fk_chat_session_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "chat_message",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    function_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    function_call_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_message_pkey", x => x.message_id);
                    table.ForeignKey(
                        name: "fk_chat_message_session",
                        column: x => x.session_id,
                        principalTable: "chat_session",
                        principalColumn: "session_id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_chat_message_created_at",
                table: "chat_message",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_chat_message_session_created_at",
                table: "chat_message",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_chat_message_session_id",
                table: "chat_message",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_session_is_active",
                table: "chat_session",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_chat_session_user_id",
                table: "chat_session",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "chat_session");
        }
    }
}
