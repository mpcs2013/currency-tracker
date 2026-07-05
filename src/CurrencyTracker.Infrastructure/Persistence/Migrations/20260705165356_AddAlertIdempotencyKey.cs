using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurrencyTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_alerts_rule_id", table: "alerts");

            migrationBuilder.AddColumn<DateOnly>(
                name: "as_of_date",
                table: "alerts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1)
            );

            migrationBuilder.CreateIndex(
                name: "ix_alerts_rule_id_as_of_date",
                table: "alerts",
                columns: new[] { "rule_id", "as_of_date" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_alerts_rule_id_as_of_date", table: "alerts");

            migrationBuilder.DropColumn(name: "as_of_date", table: "alerts");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_rule_id",
                table: "alerts",
                column: "rule_id"
            );
        }
    }
}
