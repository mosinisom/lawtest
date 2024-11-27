using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lawtest.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Tests_TestId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tests");

            migrationBuilder.RenameColumn(
                name: "SubCategory",
                table: "Tests",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "LawBranchId",
                table: "Tests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TestId",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tests_LawBranchId",
                table: "Tests",
                column: "LawBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Tests_TestId",
                table: "Questions",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_LawBranches_LawBranchId",
                table: "Tests",
                column: "LawBranchId",
                principalTable: "LawBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Tests_TestId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_LawBranches_LawBranchId",
                table: "Tests");

            migrationBuilder.DropIndex(
                name: "IX_Tests_LawBranchId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "LawBranchId",
                table: "Tests");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tests",
                newName: "SubCategory");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "TestId",
                table: "Questions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Tests_TestId",
                table: "Questions",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id");
        }
    }
}
