using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitAndValidationToAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "attributes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFilterable",
                table: "attributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSearchable",
                table: "attributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "attributes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "attributes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "attributes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "IsFilterable",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "IsSearchable",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "attributes");
        }
    }
}
