using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class policy11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyCategories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxMembersAllowed = table.Column<int>(type: "int", nullable: false),
                    PremiumBasedOn = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "PolicyTiers",
                columns: table => new
                {
                    TierId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaseCoverageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BasePremiumAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidityInYears = table.Column<int>(type: "int", nullable: false),
                    Benefits = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyTiers", x => x.TierId);
                    table.ForeignKey(
                        name: "FK_PolicyTiers_PolicyCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "PolicyCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyTiers_CategoryId",
                table: "PolicyTiers",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyTiers");

            migrationBuilder.DropTable(
                name: "PolicyCategories");
        }
    }
}
