using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JPGStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Blocks",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => new { x.Id, x.Number, x.Slot });
                });

            migrationBuilder.CreateTable(
                name: "ListingsByAddress",
                schema: "public",
                columns: table => new
                {
                    OwnerAddress = table.Column<string>(type: "text", nullable: false),
                    TxHash = table.Column<string>(type: "text", nullable: false),
                    TxIndex = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UtxoStatus = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount_Coin = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Amount_MultiAssetJson = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    EstimatedTotalListingValue = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    EstimatedFromListingValue = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    EstimatedToListingValue = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BuyerAddress = table.Column<string>(type: "text", nullable: true),
                    SpentTxHash = table.Column<string>(type: "text", nullable: true),
                    SellerPayoutValue_Coin = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    SellerPayoutValue_MultiAssetJson = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    ListingDatumCbor = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingsByAddress", x => new { x.OwnerAddress, x.TxHash, x.Slot, x.TxIndex, x.UtxoStatus, x.Status });
                });

            migrationBuilder.CreateTable(
                name: "ReducerStates",
                schema: "public",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReducerStates", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "TransactionOutputs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Amount_Coin = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Amount_MultiAssetJson = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    Datum_Type = table.Column<int>(type: "integer", nullable: true),
                    Datum_Data = table.Column<byte[]>(type: "bytea", nullable: true),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOutputs", x => new { x.Id, x.Index });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Slot",
                schema: "public",
                table: "Blocks",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_EstimatedFromListingValue",
                schema: "public",
                table: "ListingsByAddress",
                column: "EstimatedFromListingValue");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_EstimatedTotalListingValue",
                schema: "public",
                table: "ListingsByAddress",
                column: "EstimatedTotalListingValue");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_OwnerAddress",
                schema: "public",
                table: "ListingsByAddress",
                column: "OwnerAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_Slot",
                schema: "public",
                table: "ListingsByAddress",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_Status",
                schema: "public",
                table: "ListingsByAddress",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_TxHash",
                schema: "public",
                table: "ListingsByAddress",
                column: "TxHash");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_TxIndex",
                schema: "public",
                table: "ListingsByAddress",
                column: "TxIndex");

            migrationBuilder.CreateIndex(
                name: "IX_ListingsByAddress_UtxoStatus",
                schema: "public",
                table: "ListingsByAddress",
                column: "UtxoStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOutputs_Slot",
                schema: "public",
                table: "TransactionOutputs",
                column: "Slot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blocks",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ListingsByAddress",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ReducerStates",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TransactionOutputs",
                schema: "public");
        }
    }
}
