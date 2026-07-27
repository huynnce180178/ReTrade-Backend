using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RetradeBE.Data;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260727193000_EnhanceChatRoomsAndMessageDeletion")]
    public partial class EnhanceChatRoomsAndMessageDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "deleted_for_receiver",
                table: "chat",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "deleted_for_sender",
                table: "chat",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_recalled",
                table: "chat",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "recalled_at",
                table: "chat",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "room_type",
                table: "chat_room",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                defaultValue: "Product");

            migrationBuilder.Sql("""
                UPDATE chat_room
                SET room_type = CASE
                    WHEN product_id IS NULL THEN 'Business'
                    ELSE 'Product'
                END
                WHERE room_type IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "idx_chat_room_type",
                table: "chat_room",
                column: "room_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_chat_room_type",
                table: "chat_room");

            migrationBuilder.DropColumn(
                name: "deleted_for_receiver",
                table: "chat");

            migrationBuilder.DropColumn(
                name: "deleted_for_sender",
                table: "chat");

            migrationBuilder.DropColumn(
                name: "is_recalled",
                table: "chat");

            migrationBuilder.DropColumn(
                name: "recalled_at",
                table: "chat");

            migrationBuilder.DropColumn(
                name: "room_type",
                table: "chat_room");
        }
    }
}
