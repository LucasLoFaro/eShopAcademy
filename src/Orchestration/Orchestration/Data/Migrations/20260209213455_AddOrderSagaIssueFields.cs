using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Orchestration.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSagaIssueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssueDetails",
                table: "order_saga_state",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueReportedAt",
                table: "order_saga_state",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                table: "order_saga_state",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssueDetails",
                table: "order_saga_state");

            migrationBuilder.DropColumn(
                name: "IssueReportedAt",
                table: "order_saga_state");

            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "order_saga_state");
        }
    }
}
