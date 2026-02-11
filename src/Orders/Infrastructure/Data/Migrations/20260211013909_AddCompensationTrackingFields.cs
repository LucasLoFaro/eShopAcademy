using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensationTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Payment_ExpiredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Payment_RefundedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Shipping_ReturnedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Shipping_TrackingUrl",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Stock_ReleasedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payment_ExpiredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_RefundedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Shipping_ReturnedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Shipping_TrackingUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Stock_ReleasedAt",
                table: "Orders");
        }
    }
}
