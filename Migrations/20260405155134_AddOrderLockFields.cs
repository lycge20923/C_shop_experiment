using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace testweb.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Orders");
        }
    }
}
