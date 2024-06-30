using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crtmgrz.Migrations
{
    /// <inheritdoc />
    public partial class CertificatesDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotAfter",
                table: "Certificates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotBefore",
                table: "Certificates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotAfter",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "NotBefore",
                table: "Certificates");
        }
    }
}
