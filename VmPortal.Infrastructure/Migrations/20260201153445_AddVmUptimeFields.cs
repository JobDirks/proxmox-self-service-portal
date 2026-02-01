using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVmUptimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastStatusChangeAt",
                table: "Vms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalRunTimeSeconds",
                table: "Vms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastStatusChangeAt",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "TotalRunTimeSeconds",
                table: "Vms");
        }
    }
}
