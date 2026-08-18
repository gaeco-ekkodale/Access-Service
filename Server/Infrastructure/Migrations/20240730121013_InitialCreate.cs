using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.AccessService.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accessright",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    guideline_classification_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    usergroup_id = table.Column<Guid>(type: "uuid", maxLength: 40, nullable: false),
                    usecase_id = table.Column<Guid>(type: "uuid", maxLength: 40, nullable: false),
                    guidline_classification_property_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    right = table.Column<int>(type: "integer", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessright", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accessright");
        }
    }
}
