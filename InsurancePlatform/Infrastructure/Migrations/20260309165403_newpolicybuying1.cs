using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newpolicybuying1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "PolicyApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AlcoholHabit",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualIncome",
                table: "PolicyApplications",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Profession",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SmokingHabit",
                table: "PolicyApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TravelKmPerMonth",
                table: "PolicyApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CloudKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_PolicyApplications_PolicyApplicationId",
                        column: x => x.PolicyApplicationId,
                        principalTable: "PolicyApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExistingHealthConditions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_PolicyApplications_PolicyApplicationId",
                        column: x => x.PolicyApplicationId,
                        principalTable: "PolicyApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NomineeDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomineeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomineePhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomineeEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IFSC = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NomineeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NomineeDetails_PolicyApplications_PolicyApplicationId",
                        column: x => x.PolicyApplicationId,
                        principalTable: "PolicyApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_PolicyApplicationId",
                table: "ApplicationDocuments",
                column: "PolicyApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_PolicyApplicationId",
                table: "FamilyMembers",
                column: "PolicyApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_NomineeDetails_PolicyApplicationId",
                table: "NomineeDetails",
                column: "PolicyApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "NomineeDetails");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "AlcoholHabit",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "AnnualIncome",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "Profession",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "SmokingHabit",
                table: "PolicyApplications");

            migrationBuilder.DropColumn(
                name: "TravelKmPerMonth",
                table: "PolicyApplications");
        }
    }
}
