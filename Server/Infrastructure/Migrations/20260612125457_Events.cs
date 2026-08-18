using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "service_id",
                table: "guideline_version",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_version_service_id",
                table: "guideline_version",
                column: "service_id",
                unique: true,
                filter: "service_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_guideline_version_service_id",
                table: "guideline_version");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "guideline_version");
        }
    }
}
