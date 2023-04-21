﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mercadona.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class MercadonaModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offer",
                columns: table => new
                {
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offer", x => new { x.StartDate, x.EndDate, x.Percentage });
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Image = table.Column<byte[]>(type: "bytea", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferProduct",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "integer", nullable: false),
                    OffersStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OffersEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OffersPercentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferProduct", x => new { x.ProductsId, x.OffersStartDate, x.OffersEndDate, x.OffersPercentage });
                    table.ForeignKey(
                        name: "FK_OfferProduct_Offer_OffersStartDate_OffersEndDate_OffersPerc~",
                        columns: x => new { x.OffersStartDate, x.OffersEndDate, x.OffersPercentage },
                        principalTable: "Offer",
                        principalColumns: new[] { "StartDate", "EndDate", "Percentage" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferProduct_Product_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferProduct_OffersStartDate_OffersEndDate_OffersPercentage",
                table: "OfferProduct",
                columns: new[] { "OffersStartDate", "OffersEndDate", "OffersPercentage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferProduct");

            migrationBuilder.DropTable(
                name: "Offer");

            migrationBuilder.DropTable(
                name: "Product");
        }
    }
}
