using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingOrderStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_CreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_ModifiedAt",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "TrackingNumber",
                table: "Orders",
                newName: "shipping_tracking_number");

            migrationBuilder.RenameColumn(
                name: "ShippedAt",
                table: "Orders",
                newName: "shipping_shipped_at");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "Orders",
                newName: "stock_reservation_id");

            migrationBuilder.RenameColumn(
                name: "ReadyForPickupAt",
                table: "Orders",
                newName: "shipping_ready_for_pickup_at");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "Orders",
                newName: "payment_paid_at");

            migrationBuilder.RenameColumn(
                name: "DestinationAddress",
                table: "Orders",
                newName: "shipping_destination_address");

            migrationBuilder.RenameColumn(
                name: "DeliveredAt",
                table: "Orders",
                newName: "shipping_delivered_at");

            migrationBuilder.RenameColumn(
                name: "Carrier",
                table: "Orders",
                newName: "shipping_carrier");

            migrationBuilder.RenameColumn(
                name: "ShippingStatus",
                table: "Orders",
                newName: "shipping_status");

            migrationBuilder.RenameColumn(
                name: "Payment_PaymentURL",
                table: "Orders",
                newName: "operations_operator_name");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "Orders",
                newName: "billing_status");

            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "payment_amount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<DateTime>(
                name: "billing_billed_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "billing_invoice_id",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "operations_packed_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "stock_committed_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_billed_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "billing_invoice_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "operations_packed_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "stock_committed_at",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "stock_reservation_id",
                table: "Orders",
                newName: "ReservationId");

            migrationBuilder.RenameColumn(
                name: "shipping_tracking_number",
                table: "Orders",
                newName: "TrackingNumber");

            migrationBuilder.RenameColumn(
                name: "shipping_shipped_at",
                table: "Orders",
                newName: "ShippedAt");

            migrationBuilder.RenameColumn(
                name: "shipping_ready_for_pickup_at",
                table: "Orders",
                newName: "ReadyForPickupAt");

            migrationBuilder.RenameColumn(
                name: "shipping_destination_address",
                table: "Orders",
                newName: "DestinationAddress");

            migrationBuilder.RenameColumn(
                name: "shipping_delivered_at",
                table: "Orders",
                newName: "DeliveredAt");

            migrationBuilder.RenameColumn(
                name: "shipping_carrier",
                table: "Orders",
                newName: "Carrier");

            migrationBuilder.RenameColumn(
                name: "payment_paid_at",
                table: "Orders",
                newName: "PaidAt");

            migrationBuilder.RenameColumn(
                name: "shipping_status",
                table: "Orders",
                newName: "ShippingStatus");

            migrationBuilder.RenameColumn(
                name: "operations_operator_name",
                table: "Orders",
                newName: "Payment_PaymentURL");

            migrationBuilder.RenameColumn(
                name: "billing_status",
                table: "Orders",
                newName: "PaymentStatus");

            migrationBuilder.AlterColumn<int>(
                name: "payment_status",
                table: "Orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<double>(
                name: "payment_amount",
                table: "Orders",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "Payment_CreatedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Payment_ModifiedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
