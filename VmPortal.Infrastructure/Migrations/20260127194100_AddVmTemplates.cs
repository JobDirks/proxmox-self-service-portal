using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVmTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VmTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Node = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TemplateVmId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultCpuCores = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultMemoryMiB = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultDiskGiB = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VmTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VmTemplates_IsActive",
                table: "VmTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VmTemplates_Node_TemplateVmId",
                table: "VmTemplates",
                columns: new[] { "Node", "TemplateVmId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VmTemplates");
        }
    }
}
