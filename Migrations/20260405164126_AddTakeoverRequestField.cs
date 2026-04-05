using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace testweb.Migrations
{
    /// <inheritdoc />
    public partial class AddTakeoverRequestField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TakeoverRequestedBy",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TakeoverRequestedBy",
                table: "Orders");
        }
    }
}
