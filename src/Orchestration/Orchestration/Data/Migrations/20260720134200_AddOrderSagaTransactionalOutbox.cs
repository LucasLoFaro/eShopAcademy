using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orchestration.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSagaTransactionalOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "order_saga_state");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "order_saga_state",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            // Quartz's ADO job store is outside the EF model. Keep its PostgreSQL
            // schema in the same deployment migration as the saga/outbox storage.
            migrationBuilder.Sql(
                """
                CREATE TABLE qrtz_job_details (
                    sched_name TEXT NOT NULL,
                    job_name TEXT NOT NULL,
                    job_group TEXT NOT NULL,
                    description TEXT NULL,
                    job_class_name TEXT NOT NULL,
                    is_durable BOOL NOT NULL,
                    is_nonconcurrent BOOL NOT NULL,
                    is_update_data BOOL NOT NULL,
                    requests_recovery BOOL NOT NULL,
                    job_data BYTEA NULL,
                    PRIMARY KEY (sched_name, job_name, job_group)
                );

                CREATE TABLE qrtz_triggers (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    job_name TEXT NOT NULL,
                    job_group TEXT NOT NULL,
                    description TEXT NULL,
                    next_fire_time BIGINT NULL,
                    prev_fire_time BIGINT NULL,
                    priority INTEGER NULL,
                    trigger_state TEXT NOT NULL,
                    trigger_type TEXT NOT NULL,
                    start_time BIGINT NOT NULL,
                    end_time BIGINT NULL,
                    calendar_name TEXT NULL,
                    misfire_instr SMALLINT NULL,
                    misfire_orig_fire_time BIGINT NULL,
                    execution_group VARCHAR(200) NULL,
                    job_data BYTEA NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, job_name, job_group)
                        REFERENCES qrtz_job_details (sched_name, job_name, job_group)
                );

                CREATE TABLE qrtz_simple_triggers (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    repeat_count BIGINT NOT NULL,
                    repeat_interval BIGINT NOT NULL,
                    times_triggered BIGINT NOT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_simprop_triggers (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    str_prop_1 TEXT NULL,
                    str_prop_2 TEXT NULL,
                    str_prop_3 TEXT NULL,
                    int_prop_1 INTEGER NULL,
                    int_prop_2 INTEGER NULL,
                    long_prop_1 BIGINT NULL,
                    long_prop_2 BIGINT NULL,
                    dec_prop_1 NUMERIC NULL,
                    dec_prop_2 NUMERIC NULL,
                    bool_prop_1 BOOL NULL,
                    bool_prop_2 BOOL NULL,
                    time_zone_id TEXT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_cron_triggers (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    cron_expression TEXT NOT NULL,
                    time_zone_id TEXT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_blob_triggers (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    blob_data BYTEA NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_calendars (
                    sched_name TEXT NOT NULL,
                    calendar_name TEXT NOT NULL,
                    calendar BYTEA NOT NULL,
                    PRIMARY KEY (sched_name, calendar_name)
                );

                CREATE TABLE qrtz_paused_trigger_grps (
                    sched_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    PRIMARY KEY (sched_name, trigger_group)
                );

                CREATE TABLE qrtz_fired_triggers (
                    sched_name TEXT NOT NULL,
                    entry_id TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    instance_name TEXT NOT NULL,
                    fired_time BIGINT NOT NULL,
                    sched_time BIGINT NOT NULL,
                    priority INTEGER NOT NULL,
                    state TEXT NOT NULL,
                    job_name TEXT NULL,
                    job_group TEXT NULL,
                    is_nonconcurrent BOOL NOT NULL,
                    requests_recovery BOOL NULL,
                    execution_group VARCHAR(200) NULL,
                    PRIMARY KEY (sched_name, entry_id)
                );

                CREATE TABLE qrtz_scheduler_state (
                    sched_name TEXT NOT NULL,
                    instance_name TEXT NOT NULL,
                    last_checkin_time BIGINT NOT NULL,
                    checkin_interval BIGINT NOT NULL,
                    PRIMARY KEY (sched_name, instance_name)
                );

                CREATE TABLE qrtz_locks (
                    sched_name TEXT NOT NULL,
                    lock_name TEXT NOT NULL,
                    PRIMARY KEY (sched_name, lock_name)
                );

                CREATE INDEX idx_qrtz_j_req_recovery ON qrtz_job_details (requests_recovery);
                CREATE INDEX idx_qrtz_t_next_fire_time ON qrtz_triggers (next_fire_time);
                CREATE INDEX idx_qrtz_t_state ON qrtz_triggers (trigger_state);
                CREATE INDEX idx_qrtz_t_nft_st ON qrtz_triggers (next_fire_time, trigger_state);
                CREATE INDEX idx_qrtz_ft_trig_name ON qrtz_fired_triggers (trigger_name);
                CREATE INDEX idx_qrtz_ft_trig_group ON qrtz_fired_triggers (trigger_group);
                CREATE INDEX idx_qrtz_ft_trig_nm_gp ON qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
                CREATE INDEX idx_qrtz_ft_trig_inst_name ON qrtz_fired_triggers (instance_name);
                CREATE INDEX idx_qrtz_ft_job_name ON qrtz_fired_triggers (job_name);
                CREATE INDEX idx_qrtz_ft_job_group ON qrtz_fired_triggers (job_group);
                CREATE INDEX idx_qrtz_ft_job_req_recovery ON qrtz_fired_triggers (requests_recovery);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE qrtz_fired_triggers;
                DROP TABLE qrtz_paused_trigger_grps;
                DROP TABLE qrtz_scheduler_state;
                DROP TABLE qrtz_locks;
                DROP TABLE qrtz_simprop_triggers;
                DROP TABLE qrtz_simple_triggers;
                DROP TABLE qrtz_cron_triggers;
                DROP TABLE qrtz_blob_triggers;
                DROP TABLE qrtz_triggers;
                DROP TABLE qrtz_job_details;
                DROP TABLE qrtz_calendars;
                """);

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "order_saga_state");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "order_saga_state",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
