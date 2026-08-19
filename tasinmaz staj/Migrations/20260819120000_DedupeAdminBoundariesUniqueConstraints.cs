using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tasinmaz_staj.Migrations
{
    /// <inheritdoc />
    public partial class DedupeAdminBoundariesUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Mükerrer Mahalle kayıtlarını birleştir: aynı (IlceId, Ad) için
            //    en küçük Id'yi "kanonik" kabul et, Properties.MahalleId
            //    referanslarını ona taşı, fazla kayıtları sil.
            migrationBuilder.Sql(@"
                WITH dupes AS (
                    SELECT ""Id"", ""IlceId"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""IlceId"", ""Ad"") AS ""KeepId""
                    FROM ""Mahalleler""
                )
                UPDATE ""Properties"" p
                SET ""MahalleId"" = d.""KeepId""
                FROM dupes d
                WHERE p.""MahalleId"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";

                WITH dupes AS (
                    SELECT ""Id"", ""IlceId"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""IlceId"", ""Ad"") AS ""KeepId""
                    FROM ""Mahalleler""
                )
                DELETE FROM ""Mahalleler"" m
                USING dupes d
                WHERE m.""Id"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";
            ");

            // 2) Mükerrer Ilce kayıtlarını birleştir (aynı IlId + Ad).
            migrationBuilder.Sql(@"
                WITH dupes AS (
                    SELECT ""Id"", ""IlId"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""IlId"", ""Ad"") AS ""KeepId""
                    FROM ""Ilceler""
                )
                UPDATE ""Mahalleler"" m
                SET ""IlceId"" = d.""KeepId""
                FROM dupes d
                WHERE m.""IlceId"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";

                WITH dupes AS (
                    SELECT ""Id"", ""IlId"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""IlId"", ""Ad"") AS ""KeepId""
                    FROM ""Ilceler""
                )
                DELETE FROM ""Ilceler"" i
                USING dupes d
                WHERE i.""Id"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";
            ");

            // 3) Mükerrer Il kayıtlarını birleştir (aynı Ad).
            migrationBuilder.Sql(@"
                WITH dupes AS (
                    SELECT ""Id"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""Ad"") AS ""KeepId""
                    FROM ""Iller""
                )
                UPDATE ""Ilceler"" i
                SET ""IlId"" = d.""KeepId""
                FROM dupes d
                WHERE i.""IlId"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";

                WITH dupes AS (
                    SELECT ""Id"", ""Ad"",
                           MIN(""Id"") OVER (PARTITION BY ""Ad"") AS ""KeepId""
                    FROM ""Iller""
                )
                DELETE FROM ""Iller"" il
                USING dupes d
                WHERE il.""Id"" = d.""Id"" AND d.""Id"" <> d.""KeepId"";
            ");

            // 4) Bundan sonra aynı hatanın tekrar oluşmasını DB seviyesinde engelle.
            migrationBuilder.CreateIndex(
                name: "IX_Mahalleler_IlceId_Ad",
                table: "Mahalleler",
                columns: new[] { "IlceId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ilceler_IlId_Ad",
                table: "Ilceler",
                columns: new[] { "IlId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Iller_Ad",
                table: "Iller",
                column: "Ad",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mahalleler_IlceId_Ad",
                table: "Mahalleler");

            migrationBuilder.DropIndex(
                name: "IX_Ilceler_IlId_Ad",
                table: "Ilceler");

            migrationBuilder.DropIndex(
                name: "IX_Iller_Ad",
                table: "Iller");

            // Not: birleştirilen (silinen) mükerrer kayıtlar geri getirilemez.
        }
    }
}
