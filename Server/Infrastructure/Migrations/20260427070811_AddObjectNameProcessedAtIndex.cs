using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectNameProcessedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kafka_dead_letter",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    partition = table.Column<int>(type: "integer", nullable: false),
                    offset = table.Column<long>(type: "bigint", nullable: false),
                    key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    value = table.Column<string>(type: "text", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kafka_dead_letter", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guideline_version_object_name_processed_at",
                table: "guideline_version",
                columns: new[] { "object_name", "processed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_guideline_version_id_identifier",
                table: "guideline_classification",
                columns: new[] { "guideline_version_id", "identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_kafka_dead_letter_failed_at",
                table: "kafka_dead_letter",
                column: "failed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kafka_dead_letter");

            migrationBuilder.DropIndex(
                name: "IX_guideline_version_object_name_processed_at",
                table: "guideline_version");

            migrationBuilder.DropIndex(
                name: "IX_guideline_classification_guideline_version_id_identifier",
                table: "guideline_classification");
        }
    }
}
