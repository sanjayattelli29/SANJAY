using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newpolicylocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "PolicyApplications",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "PolicyApplications",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "District",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "State",
                table: "PolicyApplications");
        }
    }
}
