using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VmPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVmUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VmUserPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPrimaryOwner = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageResources = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VmUserPermissions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VmUserPermissions");
        }
    }
}
