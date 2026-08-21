using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tasinmaz_staj.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCoordinateFromProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mahalleler_IlceId_Ad",
                table: "Mahalleler");

            migrationBuilder.DropIndex(
                name: "IX_Iller_Ad",
                table: "Iller");

            migrationBuilder.DropIndex(
                name: "IX_Ilceler_IlId_Ad",
                table: "Ilceler");

            migrationBuilder.DropColumn(
                name: "Coordinate",
                table: "Properties");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_AdaNo",
                table: "Properties",
                column: "AdaNo");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ParselNo",
                table: "Properties",
                column: "ParselNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_AdaNo",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ParselNo",
                table: "Properties");

            migrationBuilder.AddColumn<string>(
                name: "Coordinate",
                table: "Properties",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mahalleler_IlceId_Ad",
                table: "Mahalleler",
                columns: new[] { "IlceId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Iller_Ad",
                table: "Iller",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ilceler_IlId_Ad",
                table: "Ilceler",
                columns: new[] { "IlId", "Ad" },
                unique: true);
        }
    }
}
