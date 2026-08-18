using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.AccessService.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class outboxtopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "topic",
                table: "outbox_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "topic",
                table: "outbox_event");
        }
    }
}
