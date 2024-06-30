using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crtmgrz.Migrations
{
    /// <inheritdoc />
    public partial class Certificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Pid = table.Column<Guid>(type: "TEXT", nullable: true),
                    Authoritative = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PrivateKeyPem = table.Column<string>(type: "TEXT", nullable: false),
                    CertificatePem = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Certificates_Pid",
                        column: x => x.Pid,
                        principalTable: "Certificates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Name",
                table: "Certificates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Pid",
                table: "Certificates",
                column: "Pid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates");
        }
    }
}
