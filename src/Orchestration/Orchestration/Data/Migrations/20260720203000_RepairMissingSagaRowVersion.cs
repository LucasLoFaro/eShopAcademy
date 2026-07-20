using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestration.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(OrderSagaDbContext))]
[Migration("20260720203000_RepairMissingSagaRowVersion")]
public partial class RepairMissingSagaRowVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE order_saga_state
            ADD COLUMN IF NOT EXISTS "RowVersion" bytea NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE order_saga_state
            DROP COLUMN IF EXISTS "RowVersion";
            """);
    }
}
