using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GuidelineRelationalProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guideline_version",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guideline_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    object_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    bucket_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    etag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    mappings_json = table.Column<string>(type: "text", nullable: true),
                    complex_data_json = table.Column<string>(type: "text", nullable: true),
                    domain_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guideline_version", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guideline_classification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guideline_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classification_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    relations_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guideline_classification", x => x.id);
                    table.ForeignKey(
                        name: "FK_guideline_classification_guideline_version_guideline_versio~",
                        column: x => x.guideline_version_id,
                        principalTable: "guideline_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guideline_property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guideline_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    storage_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit_abbreviation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    property_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    extra_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guideline_property", x => x.id);
                    table.ForeignKey(
                        name: "FK_guideline_property_guideline_version_guideline_version_id",
                        column: x => x.guideline_version_id,
                        principalTable: "guideline_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guideline_property_set",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guideline_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_set_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guideline_property_set", x => x.id);
                    table.ForeignKey(
                        name: "FK_guideline_property_set_guideline_version_guideline_version_~",
                        column: x => x.guideline_version_id,
                        principalTable: "guideline_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guideline_classification_property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guideline_classification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classification_property_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    property_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    property_set_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_number = table.Column<int>(type: "integer", nullable: false),
                    is_readonly = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    assignment_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guideline_classification_property", x => x.id);
                    table.ForeignKey(
                        name: "FK_guideline_classification_property_guideline_classification_~",
                        column: x => x.guideline_classification_id,
                        principalTable: "guideline_classification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_guideline_version_id_classificatio~",
                table: "guideline_classification",
                columns: new[] { "guideline_version_id", "classification_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_property_guideline_classification_~",
                table: "guideline_classification_property",
                columns: new[] { "guideline_classification_id", "classification_property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_property_guideline_version_id_property_id",
                table: "guideline_property",
                columns: new[] { "guideline_version_id", "property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_property_set_guideline_version_id_property_set_id",
                table: "guideline_property_set",
                columns: new[] { "guideline_version_id", "property_set_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_version_object_name_etag",
                table: "guideline_version",
                columns: new[] { "object_name", "etag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_version_processed_at",
                table: "guideline_version",
                column: "processed_at",
                descending: new[] { true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guideline_version_processed_at",
                table: "guideline_version");

            migrationBuilder.DropTable(
                name: "guideline_classification_property");

            migrationBuilder.DropTable(
                name: "guideline_property");

            migrationBuilder.DropTable(
                name: "guideline_property_set");

            migrationBuilder.DropTable(
                name: "guideline_classification");

            migrationBuilder.DropTable(
                name: "guideline_version");
        }
    }
}
