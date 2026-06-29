using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class BackfillAuctionDepositTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO auction_deposit_transaction (
                    auction_deposit_transaction_id,
                    auction_deposit_id,
                    auction_id,
                    user_id,
                    transaction_type,
                    amount,
                    status,
                    note,
                    created_at,
                    completed_at
                )
                SELECT
                    LEFT('ADTX_BACKFILL_' || auction_deposit_id, 100),
                    auction_deposit_id,
                    auction_id,
                    user_id,
                    'InitialDeposit',
                    deposit_amount,
                    CASE
                        WHEN status IN ('Paid', 'AppliedToOrder', 'RefundPending', 'Refunded') THEN 'Success'
                        WHEN status = 'Failed' THEN 'Failed'
                        ELSE 'Pending'
                    END,
                    'Backfilled from existing auction deposit total.',
                    created_at,
                    CASE
                        WHEN status IN ('Paid', 'AppliedToOrder', 'RefundPending', 'Refunded', 'Failed') THEN created_at
                        ELSE NULL
                    END
                FROM auction_deposit
                WHERE deposit_amount IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM auction_deposit_transaction adt
                      WHERE adt.auction_deposit_transaction_id = LEFT('ADTX_BACKFILL_' || auction_deposit.auction_deposit_id, 100)
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM auction_deposit_transaction
                WHERE note = 'Backfilled from existing auction deposit total.';
            ");
        }
    }
}
