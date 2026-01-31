using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatePerInstanceLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxCpuCores",
                table: "VmTemplates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDiskGiB",
                table: "VmTemplates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxMemoryMiB",
                table: "VmTemplates",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCpuCores",
                table: "VmTemplates");

            migrationBuilder.DropColumn(
                name: "MaxDiskGiB",
                table: "VmTemplates");

            migrationBuilder.DropColumn(
                name: "MaxMemoryMiB",
                table: "VmTemplates");
        }
    }
}
