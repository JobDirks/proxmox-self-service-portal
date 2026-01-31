using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVmPerInstanceLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxCpuCores",
                table: "Vms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDiskGiB",
                table: "Vms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxMemoryMiB",
                table: "Vms",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCpuCores",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "MaxDiskGiB",
                table: "Vms");

            migrationBuilder.DropColumn(
                name: "MaxMemoryMiB",
                table: "Vms");
        }
    }
}
