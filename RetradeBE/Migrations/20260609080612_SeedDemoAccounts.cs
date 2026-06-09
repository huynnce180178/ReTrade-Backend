using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_ADMIN",
                column: "password_hash",
                value: "$2a$11$SE.uBS6qCHQm0lTXK/ouJulwWDPVNdEzpinVp7/Y401j3QEGzptzm");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_BUYER",
                column: "password_hash",
                value: "$2a$11$sLI8kcGyDJOcMc/lHYgumu8D34BUc0AWTP7V0JBSSXulDYtGH0xoO");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_SELLER",
                column: "password_hash",
                value: "$2a$11$WQkmSi32N2ZjB38Ebce9A.SwtEo7crYPLFw6iK7whz8tmXuaWMuN2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_ADMIN",
                column: "password_hash",
                value: "$2a$11$YnOxdnVxUbOOwfOaEuGLnuzYQWe/73umNN5fSD68t0hDR8WqQQtjC");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_BUYER",
                column: "password_hash",
                value: "$2a$11$/hUbiIcJfH7DBCIG/.T21eF/1aNcWzEOH0aMPGbzrBc2zO6h7vxlS");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: "ACC_SELLER",
                column: "password_hash",
                value: "$2a$11$tp3SpHO62m9lZd7zgHAlnuI9ajVzArRHsjJPMiGkmMceDTbo/Kro2");
        }
    }
}
