using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BirthdayNotificationSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnniversaryNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AnniversaryDate",
                table: "Users",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnniversaryDate",
                table: "Users");
        }
    }
}
