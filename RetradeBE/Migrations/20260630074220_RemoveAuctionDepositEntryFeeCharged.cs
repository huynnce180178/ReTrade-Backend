using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuctionDepositEntryFeeCharged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "entry_fee_charged",
                table: "auction_deposit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "entry_fee_charged",
                table: "auction_deposit",
                type: "boolean",
                nullable: true,
                defaultValue: false);
        }
    }
}
