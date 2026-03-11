using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class claimsenahance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccidentCause",
                table: "InsuranceClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdmissionDate",
                table: "InsuranceClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyPartInjured",
                table: "InsuranceClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DischargeDate",
                table: "InsuranceClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMedicalCost",
                table: "InsuranceClaims",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FirNumber",
                table: "InsuranceClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HospitalBill",
                table: "InsuranceClaims",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "IncidentTime",
                table: "InsuranceClaims",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InjuryType",
                table: "InsuranceClaims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Medicines",
                table: "InsuranceClaims",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherExpenses",
                table: "InsuranceClaims",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PoliceCaseFiled",
                table: "InsuranceClaims",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccidentCause",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "AdmissionDate",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "BodyPartInjured",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "DischargeDate",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "EstimatedMedicalCost",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "FirNumber",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "HospitalBill",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "IncidentTime",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "InjuryType",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "Medicines",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "OtherExpenses",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "PoliceCaseFiled",
                table: "InsuranceClaims");
        }
    }
}
