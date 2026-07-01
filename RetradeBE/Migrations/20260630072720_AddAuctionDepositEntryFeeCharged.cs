using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionDepositEntryFeeCharged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "entry_fee_charged",
                table: "auction_deposit",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE auction_deposit
                SET entry_fee_charged = TRUE
                WHERE status IN ('Paid', 'AppliedToOrder', 'RefundPending', 'Refunded');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "entry_fee_charged",
                table: "auction_deposit");
        }
    }
}
