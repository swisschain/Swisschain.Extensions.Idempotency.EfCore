using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tests.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tests");

            migrationBuilder.CreateSequence(
                name: "id_generator_some_sequence",
                schema: "tests");

            migrationBuilder.CreateSequence(
                name: "id_generator_started_from_100",
                schema: "tests",
                startValue: 100L,
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "id_generator_test_entity",
                schema: "tests");

            migrationBuilder.CreateTable(
                name: "id_generator",
                schema: "tests",
                columns: table => new
                {
                    IdempotencyId = table.Column<string>(nullable: false),
                    Value = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_id_generator", x => x.IdempotencyId);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "tests",
                columns: table => new
                {
                    IdempotencyId = table.Column<string>(nullable: false),
                    Response = table.Column<string>(nullable: true),
                    Events = table.Column<string>(nullable: true),
                    Commands = table.Column<string>(nullable: true),
                    IsDispatched = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.IdempotencyId);
                });

            migrationBuilder.CreateTable(
                name: "test_entities",
                schema: "tests",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_entities", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "id_generator",
                schema: "tests");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "tests");

            migrationBuilder.DropTable(
                name: "test_entities",
                schema: "tests");

            migrationBuilder.DropSequence(
                name: "id_generator_some_sequence",
                schema: "tests");

            migrationBuilder.DropSequence(
                name: "id_generator_started_from_100",
                schema: "tests");

            migrationBuilder.DropSequence(
                name: "id_generator_test_entity",
                schema: "tests");
        }
    }
}
