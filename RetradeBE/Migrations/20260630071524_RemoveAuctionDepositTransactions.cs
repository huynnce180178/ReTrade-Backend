using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuctionDepositTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_deposit_transaction");

            migrationBuilder.CreateTable(
                name: "review_report",
                columns: table => new
                {
                    review_report_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    review_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_report");

            migrationBuilder.CreateTable(
                name: "auction_deposit_transaction",
                columns: table => new
                {
                    auction_deposit_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    auction_deposit_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider_transaction_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    transaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auction_deposit_transaction_pkey", x => x.auction_deposit_transaction_id);
                    table.ForeignKey(
                        name: "fk_adt_auction",
                        column: x => x.auction_id,
                        principalTable: "auction",
                        principalColumn: "auction_id");
                    table.ForeignKey(
                        name: "fk_adt_deposit",
                        column: x => x.auction_deposit_id,
                        principalTable: "auction_deposit",
                        principalColumn: "auction_deposit_id");
                    table.ForeignKey(
                        name: "fk_adt_payment",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "payment_id");
                    table.ForeignKey(
                        name: "fk_adt_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_transaction_auction_id",
                table: "auction_deposit_transaction",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_transaction_deposit_id",
                table: "auction_deposit_transaction",
                column: "auction_deposit_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_transaction_payment_id",
                table: "auction_deposit_transaction",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_transaction_user_id",
                table: "auction_deposit_transaction",
                column: "user_id");
        }
    }
}
