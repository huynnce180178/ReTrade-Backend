using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionDepositTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auction_deposit_transaction",
                columns: table => new
                {
                    auction_deposit_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    auction_deposit_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    provider_transaction_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_deposit_transaction");
        }
    }
}
