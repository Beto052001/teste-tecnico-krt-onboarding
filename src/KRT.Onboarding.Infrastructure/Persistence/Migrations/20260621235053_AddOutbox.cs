using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRT.Onboarding.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    ocorrido_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultimo_erro = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pendentes",
                table: "outbox_messages",
                columns: new[] { "processado_em_utc", "ocorrido_em_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
