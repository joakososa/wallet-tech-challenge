using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "numero_socio_seq");

            migrationBuilder.CreateTable(
                name: "socios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_did = table.Column<string>(type: "text", nullable: false),
                    numero_socio = table.Column<long>(type: "bigint", nullable: false),
                    dni = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    apellido = table.Column<string>(type: "text", nullable: false),
                    categoria = table.Column<string>(type: "text", nullable: false),
                    foto = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_socios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vc_id = table.Column<string>(type: "text", nullable: false),
                    socio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    apellido = table.Column<string>(type: "text", nullable: false),
                    dni = table.Column<string>(type: "text", nullable: false),
                    numero_socio = table.Column<long>(type: "bigint", nullable: false),
                    categoria = table.Column<string>(type: "text", nullable: false),
                    foto = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    document = table.Column<string>(type: "json", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_credentials_socios_socio_id",
                        column: x => x.socio_id,
                        principalTable: "socios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_socio_id",
                table: "credentials",
                column: "socio_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_valid_from",
                table: "credentials",
                column: "valid_from",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_credentials_vc_id",
                table: "credentials",
                column: "vc_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_socios_dni",
                table: "socios",
                column: "dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_socios_numero_socio",
                table: "socios",
                column: "numero_socio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_socios_subject_did",
                table: "socios",
                column: "subject_did",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credentials");

            migrationBuilder.DropTable(
                name: "socios");

            migrationBuilder.DropSequence(
                name: "numero_socio_seq");
        }
    }
}
