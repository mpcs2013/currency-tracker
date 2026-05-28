using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurrencyTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @base = table.Column<string>(
                        name: "base",
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    quote = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    threshold_percent = table.Column<decimal>(
                        type: "numeric(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false
                    ),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_rules", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_rate = table.Column<decimal>(
                        type: "numeric(18,8)",
                        precision: 18,
                        scale: 8,
                        nullable: false
                    ),
                    current_rate = table.Column<decimal>(
                        type: "numeric(18,8)",
                        precision: 18,
                        scale: 8,
                        nullable: false
                    ),
                    observed_change_percent = table.Column<decimal>(
                        type: "numeric(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false
                    ),
                    fired_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    code = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    numeric_code = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.code);
                }
            );

            migrationBuilder.CreateTable(
                name: "rate_snapshots",
                columns: table => new
                {
                    @base = table.Column<string>(
                        name: "base",
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_snapshots", x => new { x.@base, x.as_of });
                }
            );

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    quote = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    snapshot_base = table.Column<string>(
                        type: "character varying(3)",
                        nullable: false
                    ),
                    snapshot_as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    @base = table.Column<string>(
                        name: "base",
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    rate = table.Column<decimal>(
                        type: "numeric(18,8)",
                        precision: 18,
                        scale: 8,
                        nullable: false
                    ),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_exchange_rates",
                        x => new
                        {
                            x.snapshot_base,
                            x.snapshot_as_of,
                            x.quote,
                        }
                    );
                    table.ForeignKey(
                        name: "fk_exchange_rates_rate_snapshots_snapshot_base_snapshot_as_of",
                        columns: x => new { x.snapshot_base, x.snapshot_as_of },
                        principalTable: "rate_snapshots",
                        principalColumns: new[] { "base", "as_of" },
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_alert_rules_owner_id",
                table: "alert_rules",
                column: "owner_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_alerts_rule_id",
                table: "alerts",
                column: "rule_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "alert_rules");

            migrationBuilder.DropTable(name: "alerts");

            migrationBuilder.DropTable(name: "currencies");

            migrationBuilder.DropTable(name: "exchange_rates");

            migrationBuilder.DropTable(name: "rate_snapshots");
        }
    }
}
