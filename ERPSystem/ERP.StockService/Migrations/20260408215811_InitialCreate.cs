using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.StockService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonEntres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FournisseurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonEntres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    Padding = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonNumbers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonRetours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonRetours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonSorties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonSorties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LigneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PieceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    StockBefore = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    StockAfter = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceService = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceOperation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LigneEntres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BonEntreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneEntres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneEntres_BonEntres_BonEntreId",
                        column: x => x.BonEntreId,
                        principalTable: "BonEntres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LigneRetours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BonRetourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Remarque = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneRetours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneRetours_BonRetours_BonRetourId",
                        column: x => x.BonRetourId,
                        principalTable: "BonRetours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LigneSorties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BonSortieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneSorties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneSorties_BonSorties_BonSortieId",
                        column: x => x.BonSortieId,
                        principalTable: "BonSorties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonEntres_Numero",
                table: "BonEntres",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonRetours_Numero",
                table: "BonRetours",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonSorties_Numero",
                table: "BonSorties",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalStocks_ArticleId",
                table: "JournalStocks",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalStocks_CreatedAt",
                table: "JournalStocks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JournalStocks_MovementType",
                table: "JournalStocks",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_LigneEntres_BonEntreId",
                table: "LigneEntres",
                column: "BonEntreId");

            migrationBuilder.CreateIndex(
                name: "IX_LigneRetours_BonRetourId",
                table: "LigneRetours",
                column: "BonRetourId");

            migrationBuilder.CreateIndex(
                name: "IX_LigneSorties_BonSortieId",
                table: "LigneSorties",
                column: "BonSortieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonNumbers");

            migrationBuilder.DropTable(
                name: "JournalStocks");

            migrationBuilder.DropTable(
                name: "LigneEntres");

            migrationBuilder.DropTable(
                name: "LigneRetours");

            migrationBuilder.DropTable(
                name: "LigneSorties");

            migrationBuilder.DropTable(
                name: "BonEntres");

            migrationBuilder.DropTable(
                name: "BonRetours");

            migrationBuilder.DropTable(
                name: "BonSorties");
        }
    }
}
