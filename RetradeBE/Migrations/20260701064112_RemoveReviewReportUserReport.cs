using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReviewReportUserReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_user",
                table: "Order");

            migrationBuilder.DropTable(
                name: "review_report");

            migrationBuilder.DropTable(
                name: "user_report");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "wishlist",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "User",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "product_attribute",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "product",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Order",
                newName: "buyer_id");

            migrationBuilder.RenameIndex(
                name: "IX_Order_user_id",
                table: "Order",
                newName: "IX_Order_buyer_id");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "notification",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "chat_room",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "chat",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "attributes",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "address",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "account",
                newName: "IsDeleted");

            migrationBuilder.AddColumn<string>(
                name: "product_id",
                table: "user_favorite",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "report",
                columns: table => new
                {
                    report_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("report_pkey", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_report_reporter",
                        column: x => x.reporter_id,
                        principalTable: "User",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_product_id",
                table: "user_favorite",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_reporter_id",
                table: "report",
                column: "reporter_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_buyer",
                table: "Order",
                column: "buyer_id",
                principalTable: "User",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_favorite_product_product_id",
                table: "user_favorite",
                column: "product_id",
                principalTable: "product",
                principalColumn: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_buyer",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_user_favorite_product_product_id",
                table: "user_favorite");

            migrationBuilder.DropTable(
                name: "report");

            migrationBuilder.DropIndex(
                name: "IX_user_favorite_product_id",
                table: "user_favorite");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "user_favorite");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "wishlist",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "User",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "product_attribute",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "product",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "buyer_id",
                table: "Order",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Order_buyer_id",
                table: "Order",
                newName: "IX_Order_user_id");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "notification",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "chat_room",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "chat",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "attributes",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "address",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "account",
                newName: "is_deleted");

            migrationBuilder.CreateTable(
                name: "review_report",
                columns: table => new
                {
                    review_report_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    review_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("review_report_pkey", x => x.review_report_id);
                    table.ForeignKey(
                        name: "fk_review_report_reporter",
                        column: x => x.reporter_id,
                        principalTable: "User",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_report_review",
                        column: x => x.review_id,
                        principalTable: "review",
                        principalColumn: "review_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_report_reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_report",
                columns: table => new
                {
                    report_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_report_pkey", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_ur_reporter",
                        column: x => x.reporter_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_ur_reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_review_report_reporter_id",
                table: "review_report",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_report_review_id",
                table: "review_report",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_report_reviewed_by",
                table: "review_report",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "review_report_review_id_reporter_id_key",
                table: "review_report",
                columns: new[] { "review_id", "reporter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_report_reporter_id",
                table: "user_report",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_report_reviewed_by",
                table: "user_report",
                column: "reviewed_by");

            migrationBuilder.AddForeignKey(
                name: "fk_order_user",
                table: "Order",
                column: "user_id",
                principalTable: "User",
                principalColumn: "user_id");
        }
    }
}
