using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestoBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationUserHide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenByUser",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHiddenByUser",
                table: "Reservations");
        }
    }
}