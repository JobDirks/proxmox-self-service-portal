using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVmLifecycleFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Vms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAt",
                table: "Vms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "Vms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "Vms");
        }
    }
}
