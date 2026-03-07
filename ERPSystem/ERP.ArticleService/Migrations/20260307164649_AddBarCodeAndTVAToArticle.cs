using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.ArticleService.Migrations
{
    /// <inheritdoc />
    public partial class AddBarCodeAndTVAToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Articles",
                newName: "CodeRef");

            migrationBuilder.AddColumn<decimal>(
                name: "TVA",
                table: "Categories",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BarCode",
                table: "Articles",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TVA",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TVA",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "BarCode",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "TVA",
                table: "Articles");

            migrationBuilder.RenameColumn(
                name: "CodeRef",
                table: "Articles",
                newName: "Code");
        }
    }
}
