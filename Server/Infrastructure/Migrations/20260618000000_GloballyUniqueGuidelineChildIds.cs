using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GloballyUniqueGuidelineChildIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop composite unique indexes — ClassificationId, PropertyId, PropertySetId are URI-based
            // and globally unique; GuidelineVersionId stays as FK for cascade delete only.
            migrationBuilder.DropIndex(
                name: "IX_guideline_classification_guideline_version_id_classificatio~",
                table: "guideline_classification");

            migrationBuilder.DropIndex(
                name: "IX_guideline_property_guideline_version_id_property_id",
                table: "guideline_property");

            migrationBuilder.DropIndex(
                name: "IX_guideline_property_set_guideline_version_id_property_set_id",
                table: "guideline_property_set");

            migrationBuilder.DropIndex(
                name: "IX_guideline_classification_guideline_version_id_identifier",
                table: "guideline_classification");

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_classification_id",
                table: "guideline_classification",
                column: "classification_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_property_property_id",
                table: "guideline_property",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_property_set_property_set_id",
                table: "guideline_property_set",
                column: "property_set_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_identifier",
                table: "guideline_classification",
                column: "identifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guideline_classification_classification_id",
                table: "guideline_classification");

            migrationBuilder.DropIndex(
                name: "IX_guideline_property_property_id",
                table: "guideline_property");

            migrationBuilder.DropIndex(
                name: "IX_guideline_property_set_property_set_id",
                table: "guideline_property_set");

            migrationBuilder.DropIndex(
                name: "IX_guideline_classification_identifier",
                table: "guideline_classification");

            migrationBuilder.CreateIndex(
                name: "IX_guideline_classification_guideline_version_id_classificatio~",
                table: "guideline_classification",
                columns: new[] { "guideline_version_id", "classification_id" },
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
                name: "IX_guideline_classification_guideline_version_id_identifier",
                table: "guideline_classification",
                columns: new[] { "guideline_version_id", "identifier" });
        }
    }
}
